from promptflow import tool
from langchain.docstore.document import Document
from langchain.chains.summarize import load_summarize_chain
from promptflow.connections import AzureOpenAIConnection
from langchain.prompts import PromptTemplate
from langchain.llms.openai import AzureOpenAI, OpenAI
from langchain.chat_models import AzureChatOpenAI, ChatOpenAI
import openai

@tool
def langchain_summarization(chunks:any,azure_open_ai_connection: AzureOpenAIConnection):
  openai.api_type = azure_open_ai_connection.api_type
  openai.api_base = azure_open_ai_connection.api_base
  openai.api_version = azure_open_ai_connection.api_version
  openai.api_key = azure_open_ai_connection.api_key
  print(len(chunks))
  docs = [Document(page_content=chunk) for chunk in chunks]
  llm = AzureChatOpenAI(
                    openai_api_base=openai.api_base,
                    openai_api_version=openai.api_version,
                    deployment_name="ChatGPT-OpenAI",
                    temperature=0.3,
                    openai_api_key=openai.api_key,
                    openai_api_type="azure",
                    max_tokens=1000)

  chainType = "refine"
  
  if chainType == "map_reduce":
    promptTemplate = """You are an AI assistant tasked with summarizing user stories and tasks from meeting recording transcript. 
    Your summary should accurately capture the key information in the discussion 
    Your summary should avoid the casual talks and those that are not relevant to a feature specific conversations. 
    Please generate a concise and comprehensive summary about 8-10 paragraphs and maintain the continuity.  
    Ensure your summary includes the key information from the transcript like user stories, tasks and bugs.
    {text}
    """
    customPrompt = PromptTemplate(template=promptTemplate, input_variables=["text"])
    summaryChain = load_summarize_chain(llm, chain_type=chainType, combine_prompt=customPrompt)
  elif chainType == "refine":
    promptTemplate = """Write a concise summary of the following: 
    {text}
    CONCISE SUMMARY:"""
    customPrompt = PromptTemplate(template=promptTemplate, input_variables=["text"])
    refineTemplate = """You are an AI assistant tasked with refining and producing final summary.
    You are provided with the existing summary up to a certain point: {existing_answer}. 
    Your summary should accurately capture the key information in the discussion 
    Your summary should avoid the casual talks and those that are not relevant to a feature specific conversations. 
    Please generate final comprehensive summary about 8-10 paragraphs and maintain the continuity.  
    You are allowed to refine the existing summary (only if needed) with some context below.
    {text}
    If the context isn't useful, return the original summary
    Ensure your summary includes the key information from the transcript like user stories, tasks and bugs.    
    """    
    refinePrompt = PromptTemplate(
                        input_variables=["existing_answer", "text"],
                        template=refineTemplate,
                    )
    summaryChain = load_summarize_chain(llm, chain_type=chainType,question_prompt=customPrompt, refine_prompt=refinePrompt)





  #promptTemplate = """You are an AI assistant tasked with summarizing user stories and tasks from meeting recording transcript. 
  #Your summary should accurately capture the key information in the discussion 
  #Your summary should avoid the casual talks and those that are not relevant to a feature specific conversations. 
  #Please generate a concise and comprehensive summary about 8-10 paragraphs and maintain the continuity.  
  #Ensure your summary includes the key information from the transcript like user stories, tasks and bugs.
  #{text}
  #"""
  #customPrompt = PromptTemplate(template=promptTemplate, input_variables=["text"])
  #chainType = "map_reduce"
  #summaryChain = load_summarize_chain(llm, chain_type=chainType, combine_prompt=customPrompt)
  #summaryOutput = summaryChain({"input_documents": docs}, return_only_outputs=True)
  summaryOutput = summaryChain.run(docs)
  return summaryOutput
  #output = summaryOutput['output_text']
  #return output