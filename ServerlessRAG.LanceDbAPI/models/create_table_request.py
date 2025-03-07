from pydantic import BaseModel
from typing import Any, Dict, List, Optional

class CreateTableRequest(BaseModel):
    table_name: Optional[str] = None
    mode: Optional[str] = "create"
    data: Optional[List[Dict[str, Any]]] = None
