from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Any
import pandas as pd
from repository import LanceDBRepository

# Define a Pydantic model (adjust as needed)
class Item(BaseModel):
    vector: List[float]
    # add other fields if needed, e.g. "item": str, "price": float, etc.

app = FastAPI()

# Instantiate the repository (use appropriate URI from your configuration)
repo = LanceDBRepository(uri="/lancedb")  # adjust the URI accordingly
TABLE_NAME = "chunks"  # use a fixed table name, for instance

@app.get("/items")
def get_items():
    try:
        # Convert the Pandas DataFrame to a list of dicts (or JSON-serializable format)
        df: pd.DataFrame = repo.get_all(TABLE_NAME)
        return df.to_dict(orient="records")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/search")
def search_items(vector: List[float], limit: int = 10):
    try:
        df = repo.search(TABLE_NAME, query_vector=vector, limit=limit)
        return df.to_dict(orient="records")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/items")
def insert_item(item: Item):
    try:
        repo.insert(TABLE_NAME, [item.dict()])
        return {"message": "Item inserted successfully"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
