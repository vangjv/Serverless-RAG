import lancedb
import pyarrow as pa
from typing import List, Dict, Optional, Any, Literal, Union
import pandas as pd
import os

class LanceDBRepository:
    def __init__(self, uri: str) -> None:
        # Connect to LanceDB per docs. Adjust the uri as needed.
        # self.db = lancedb.connect(uri)
        self.db = lancedb.connect(
        os.environ["LanceDbContainerFolderURI"],
        storage_options={
            "account_name": os.environ["StorageAccountName"],
            "account_key": os.environ["StorageAccountKey"],
        })
    
    def get_table(self, table_name: str):
        return self.db.open_table(table_name)

    def create_table(
        self,
        table_name: str,
        schema: Optional[pa.Schema] = None,
        data: Optional[List[Dict[str, Any]]] = None,
        mode: Literal["create", "overwrite"] = "create"
    ) -> None:
        self.db.create_table(table_name, data=data, schema=schema, mode=mode)

    def insert(self, table_name: str, data: List[Dict[str, Any]]) -> None:
        table = self.get_table(table_name)
        table.add(data)

    def bulk_insert(self, table_name: str, data: List[Dict[str, Any]]) -> None:
        """
        Insert multiple items into the specified table.
        """
        table = self.get_table(table_name)
        table.add(data)

    def search(
        self,
        table_name: str,
        query_vector: List[float],
        limit: int = 10,
        where: Optional[str] = None,
        select_columns: Optional[List[str]] = None
    ) -> pd.DataFrame:
        table = self.get_table(table_name)
        qb = table.search(query_vector)
        if where:
            qb = qb.where(where)
        if select_columns:
            qb = qb.select(select_columns)
        qb = qb.limit(limit)
        return qb.to_pandas()

    def search_text(
        self,
        table_name: str,
        query: str,
        limit: int = 10,
        where: Optional[str] = None,
        select_columns: Optional[List[str]] = None
    ) -> pd.DataFrame:
        table = self.get_table(table_name)
        qb = table.search(query)  # LancDB will treat a string query as a full-text search.
        if where:
            qb = qb.where(where)
        if select_columns:
            qb = qb.select(select_columns)
        qb = qb.limit(limit)
        return qb.to_pandas()

    def create_full_text_index(
        self,
        table_name: str,
        field_names: Union[str, List[str]],
        ordering_field_names: Optional[Union[str, List[str]]] = None,
        replace: bool = False,
        writer_heap_size: Optional[int] = 1024 * 1024 * 1024,
        use_tantivy: bool = True,
        tokenizer_name: Optional[str] = None,
        with_position: bool = True
    ) -> None:
        table = self.get_table(table_name)
        table.create_fts_index(
            field_names,
            ordering_field_names=ordering_field_names,
            replace=replace,
            writer_heap_size=writer_heap_size,
            use_tantivy=use_tantivy,
            tokenizer_name=tokenizer_name,
            with_position=with_position
        )

    def create_vector_index(
        self,
        table_name: str,
        vector_column_name: Optional[str] = None,
        replace: bool = True,
        metric: str = "L2",
        num_partitions: int = 256,
        num_sub_vectors: int = 96,
        index_type: Literal['IVF_FLAT', 'IVF_PQ', 'IVF_HNSW_SQ', 'IVF_HNSW_PQ'] = "IVF_PQ",
        num_bits: int = 8,
        **kwargs: Any
    ) -> None:
        table = self.get_table(table_name)
        table.create_index(
            vector_column_name=vector_column_name,
            replace=replace,
            metric=metric,
            num_partitions=num_partitions,
            num_sub_vectors=num_sub_vectors,
            index_type=index_type,
            num_bits=num_bits,
            **kwargs
        )

    def get_all(self, table_name: str) -> pd.DataFrame:
        table = self.get_table(table_name)
        return table.to_pandas()
