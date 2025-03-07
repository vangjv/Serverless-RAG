from pydantic import BaseModel
from typing import List, Optional

from pydantic import BaseModel
from typing import Any, Dict

class Item(BaseModel):
    # Allow arbitrary fields representing the item.
    __root__: Dict[str, Any]

    class Config:
        extra = "allow"
