from promptflow import tool
from langchain.text_splitter import CharacterTextSplitter

@tool
def generate_chunks(content:str):    
    text_splitter = CharacterTextSplitter(separator="\n",chunk_size=1000,chunk_overlap=200,length_function=len)
    chunks = text_splitter.split_text(content)
    return chunks
