import api,{webTrigger} from "@forge/api";

import ForgeUI, { render, Fragment,Text, IssuePanel, useProductContext, useState,Button} from '@forge/ui';
  
  async function createJiraIssue(projectKey, projectId, issueType, summary, description,parentIssueKey) {
    try {
      const issueData = {
        fields: {        
        project: {
          id: projectId,
          key: projectKey,
        },
        issuetype: {
          name: issueType=="10003"? "Task": "Story",
          id: issueType
        },
        summary,
        description: {
          type: "doc",
          version: 1,
          content: [
            {
              type: "paragraph",
              content: [
                {
                  type: "text",
                  text: description
                }
              ]
            }
          ]
        }              
      }
    };

    if (issueType === "10003" && parentIssueKey) {      
      issueData.fields.parent = {
        key: parentIssueKey
      };
    }

      console.log(issueData);
  
      const response = await api.asApp().requestJira(`/rest/api/3/issue`, {
        method: 'POST',
        body: JSON.stringify(issueData),
        headers: {
          'Content-Type': 'application/json',
        },
      });
  
      if (response.status === 201) {
        const data = await response.json();
        console.log(data);
        if (data && data.key) {
          return data.key;
        } else {
          console.error('Error creating Jira issue: Unexpected response format');
          return null;
        }
      } else {
        const errorMessage = await response.text();
        console.error(`Error creating Jira issue: ${response.status} - ${errorMessage}`);
        return null;
      }
    } catch (error) {
      console.error('Error creating Jira issue:', error);
      return null;
    }
  }
  
  async function createChildTasksForStory(projectKey,projectId, parentIssueKey, tasks) {
    for (const task of tasks) {
      const issueType = task.issueType;
      const summary = task.description;
      const description = " Acceptance Criteria: \n"+task.acceptanceCriteria;      
  
      try {
        const response = await createJiraIssue(projectKey,projectId, issueType, summary, description,parentIssueKey);
        const childIssueKey = response;
        console.log(`Created Jira task with key: ${childIssueKey}`);        
      } catch (error) {
        console.error('Error creating Jira task:', error);
      }
    }
  }
  
  async function createJiraIssuesFromJSON(reqBody) {    
    console.log(reqBody.projectKey);
    const jsonPayload = JSON.parse(reqBody.stories);
    const stories = JSON.parse(jsonPayload.result).stories;    
    console.log(stories);
    for (const story of stories) {
      const issueType = story.issueType; // Get the issue type for the parent story
      const summary = story.description;
      const description = " Acceptance Criteria: \n"+story.acceptanceCriteria;      

      console.log(story.description);
  
      try {
        const response = await createJiraIssue(reqBody.projectKey,reqBody.projectId, issueType, summary, description,reqBody.currentIssueKey);
        console.log("story created", response);
        const parentIssueKey = response;
        console.log(`Created Jira story with key: ${parentIssueKey}`);
  
        // Create child tasks for the story
        await createChildTasksForStory(reqBody.projectKey,reqBody.projectId, parentIssueKey, story.tasks);
      } catch (error) {
        console.error('Error creating Jira issue:', error);
      }
    }
  }

const fetchAttachments = async (issueKey) => {
  try {
    const response = await api.asUser().requestJira(
      `/rest/api/3/issue/${issueKey}?expand=attachment`
    );

    if (response.status === 200) {
      const data = await response.json();      
      return data.fields.attachment;
    } else {
      // Handle error
      console.error(`Error fetching attachments: ${response.status}`);
    }
  } catch (error) {
    // Handle error
    console.error('Error:', error);
  }
};

function App() {
  const context = useProductContext();
  const selectedFileNames= [];
  const [isProcessing, setIsProcessing] = useState(false);
  const [trigger] = useState(webTrigger.getUrl("openAI-listener"));
  console.log(trigger);
  const [attachments] = useState(async () => await fetchAttachments(context.platformContext.issueKey)); 
  const projectKey = context.platformContext.projectKey;
  const projectId = context.platformContext.projectId;  

  const azureFunctionUploadToBlobUrl = "https://func-codegeistunleased.azurewebsites.net/api/FxUploadToBlob";
  const azureFunctionPushMessageToQueueUrl = "https://func-codegeistunleased.azurewebsites.net/api/FxPushMessageToQueue"

async function uploadToAzureFunction(attachment,filename) {  
  console.log(filename);  
  const attachmentData = await api.asUser().requestJira(`/rest/api/3/attachment/content/${attachment.id}`, {
    headers: {
      'Accept': 'application/json'
    }}
  );
  if (attachmentData.status === 200) {   
    const attachmentContent = await attachmentData.text();    
    const headers = {
      "Content-Type": "application/json",
      "X-Filename": filename
    };

    const attachmentUploadResponse = await api.fetch(azureFunctionUploadToBlobUrl, {
      method: "POST",
      body: JSON.stringify({ data: attachmentContent, filename: filename }),
      headers: headers,
    });
    console.log(attachmentUploadResponse.status);
    if (attachmentUploadResponse.status === 200) {
      console.log(`Uploaded ${attachment.filename} successfully`);
    } else {
      console.log("error occured");
      const errorMessage = JSON.stringify(attachmentUploadResponse);
      console.error(`Error uploading ${filename}: ${errorMessage}`);
    }
   
  } else {
    console.error(`Error getting attachment content: ${response.status} ${response.statusText}`);
    return null;
  }  

}


async function pushMessageToQueue(requestBody) {  

  const response = await api.fetch(azureFunctionPushMessageToQueueUrl, {
    method: "POST",
    body: JSON.stringify(requestBody)    
  });

  if (response.status === 200) {    
    setIsProcessing(true);        
  } else {
    console.error(`The request failed with status code: ${response.status}`);            
    const responseContent = await response.text();
    console.error(responseContent);    
  }       
}

  const [selectedAttachments, setSelectedAttachments] = useState([]);
  const toggleAttachmentSelection = (attachment) => {
    if (selectedAttachments.includes(attachment)) {
      setSelectedAttachments(
        selectedAttachments.filter((selected) => selected !== attachment)
      );
    } else {
      setSelectedAttachments([...selectedAttachments, attachment]);
    }
  };




  const logSelectedAttachments = async () => {
    console.log("Selected Attachments:", selectedAttachments);
    for (const attachmentId of selectedAttachments) {
      const attachment = attachments.find((attachment) => attachment.id === attachmentId);  
      if (attachment) {
        console.log(attachment);
        const filename = projectKey+"-"+attachment.filename;
        selectedFileNames.push(filename);
        await uploadToAzureFunction(attachment,filename);
      }
    }  
    const requestBody = {
      blobs: selectedFileNames,
      webhookUrl: trigger,
      projectKey: projectKey,
      projectId: projectId,
      currentIssueKey: context.platformContext.issueKey      
    };
    await pushMessageToQueue(requestBody);        
  };

  const isGenerateButtonVisible = selectedAttachments.length > 0;  

  console.log(selectedAttachments.length);
  console.log(isGenerateButtonVisible);

  return (
    <Fragment>            
      <Text>
      {attachments.length > 0 ? "Choose meeting transcripts:" : "No attachments found"}
      </Text>
      {attachments.map((attachment) => (
        <Fragment key={attachment.id}>
          <Button
            text={
              selectedAttachments.includes(attachment.id)
                ? `✅ ${attachment.filename}`
                : `◻️ ${attachment.filename}`
            }            
            onClick={() => toggleAttachmentSelection(attachment.id)}
          />
        </Fragment>
      ))}      
         {isGenerateButtonVisible && (
        <Button
          text={ isProcessing ? "Analyzing transcripts.." : "Generate Work Items" }
          onClick={logSelectedAttachments}
          appearance={isProcessing ? "warning": "primary"}
          disabled= {isProcessing}
        />
      )}           
    </Fragment>
  );
}

export async function listener(req) {
  try {
    console.log(req);
    const body = JSON.parse(req.body);
    console.log(body);              
    await createJiraIssuesFromJSON(body);     
    return {
      body: "Success: Message updated\n",
      headers: { "Content-Type": ["application/json"] },
      statusCode: 200,
      statusText: "OK",
    };
  } catch (error) {
    return {
      body: error + "\n",
      headers: { "Content-Type": ["application/json"] },
      statusCode: 400,
      statusText: "Bad Request",
    }
  }
}


export const run = render(  
  <IssuePanel>
    <App />   
    </IssuePanel>   
);