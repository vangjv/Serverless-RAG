from pydantic import BaseModel
from typing import List, Optional

from pydantic import BaseModel
from typing import List, Optional

class SearchQuery(BaseModel):
    vector: List[float]
    where: Optional[str] = None
    columns: Optional[List[str]] = None
    limit: Optional[int] = 10
