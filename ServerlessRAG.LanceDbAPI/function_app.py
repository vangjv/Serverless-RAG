import azure.functions as func
import logging
import json
from repository import LanceDBRepository
import pandas as pd

repo = LanceDBRepository(uri="/lancedb")  # adjust the URI as needed
TABLE_NAME = "chunks"  # use your desired table name

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

@app.route(route="{table_name}/items", methods=["GET"])
def get_items(req: func.HttpRequest) -> func.HttpResponse:
    table_name: str = req.route_params.get("table_name")
    try:
        df: pd.DataFrame = repo.get_all(table_name)
        items = df.to_dict(orient="records")
        return func.HttpResponse(
            json.dumps(items, default=lambda o: o.tolist() if hasattr(o, "tolist") else o),
            mimetype="application/json"
        )
    except Exception as e:
        return func.HttpResponse(json.dumps({"detail": str(e)}),
                                 status_code=500,
                                 mimetype="application/json")

@app.route(route="{table_name}/search", methods=["POST"])
def search_items(req: func.HttpRequest) -> func.HttpResponse:
    table_name: str = req.route_params.get("table_name")
    try:
        payload = req.get_json()
    except Exception:
        return func.HttpResponse(json.dumps({"detail": "Invalid JSON body"}),
                                 status_code=400,
                                 mimetype="application/json")

    vector = payload.get("vector")
    if not vector:
        return func.HttpResponse(json.dumps({"detail": "Missing 'vector' in JSON body"}),
                                 status_code=400,
                                 mimetype="application/json")

    try:
        if not isinstance(vector, list):
            return func.HttpResponse(
                json.dumps({"detail": "The 'vector' must be provided as a list of numbers"}),
                status_code=400,
                mimetype="application/json"
            )
        vector = [float(x) for x in vector]

        # Optional query options from the JSON body:
        where_clause = payload.get("where")       # expected as a string
        columns = payload.get("columns")            # expected as a list of column names
        limit = int(payload.get("limit", 10))
        return_vector: bool = payload.get("returnVector", True)

        df = repo.search(table_name, query_vector=vector, where=where_clause, select_columns=columns, limit=limit)
        if not return_vector:
            df.drop(columns=["vector"], errors="ignore", inplace=True)
        results = df.to_dict(orient="records")
        return func.HttpResponse(
            json.dumps(results, default=lambda o: o.tolist() if hasattr(o, "tolist") else o),
            mimetype="application/json"
        )
    except Exception as e:
        return func.HttpResponse(json.dumps({"detail": str(e)}),
                                 status_code=500,
                                 mimetype="application/json")

@app.route(route="{table_name}/search_text", methods=["POST"])
def search_text_items(req: func.HttpRequest) -> func.HttpResponse:
    table_name: str = req.route_params.get("table_name")
    try:
        payload = req.get_json()
    except Exception:
        return func.HttpResponse(
            json.dumps({"detail": "Invalid JSON body"}),
            status_code=400,
            mimetype="application/json"
        )
    
    query = payload.get("query")
    if not query:
        return func.HttpResponse(
            json.dumps({"detail": "Missing 'query' in JSON body"}),
            status_code=400,
            mimetype="application/json"
        )
    
    try:
        where_clause = payload.get("where")       # optional SQL filter
        columns = payload.get("columns")            # optional list of column names
        limit = int(payload.get("limit", 10))
        df = repo.search_text(table_name, query=query, where=where_clause, select_columns=columns, limit=limit)
        results = df.to_dict(orient="records")
        return func.HttpResponse(
            json.dumps(results, default=lambda o: o.tolist() if hasattr(o, "tolist") else o),
            mimetype="application/json"
        )
    except Exception as e:
        return func.HttpResponse(
            json.dumps({"detail": str(e)}),
            status_code=500,
            mimetype="application/json"
        )

@app.route(route="{table_name}/create_fts_index", methods=["POST"])
def create_fts_index_endpoint(req: func.HttpRequest) -> func.HttpResponse:
    table_name: str = req.route_params.get("table_name")
    try:
        payload = req.get_json()
    except Exception:
        return func.HttpResponse(
            json.dumps({"detail": "Invalid JSON body"}),
            status_code=400,
            mimetype="application/json"
        )
    # Get parameters with defaults.
    field_names = payload.get("field_names")
    if not field_names:
        return func.HttpResponse(
            json.dumps({"detail": "Missing 'field_names' in JSON body"}),
            status_code=400,
            mimetype="application/json"
        )
    ordering_field_names = payload.get("ordering_field_names")
    replace = payload.get("replace", False)
    writer_heap_size = payload.get("writer_heap_size", 1024 * 1024 * 1024)
    use_tantivy = payload.get("use_tantivy", True)
    tokenizer_name = payload.get("tokenizer_name")
    with_position = payload.get("with_position", True)
    try:
        repo.create_full_text_index(
            table_name,
            field_names,
            ordering_field_names=ordering_field_names,
            replace=replace,
            writer_heap_size=writer_heap_size,
            use_tantivy=use_tantivy,
            tokenizer_name=tokenizer_name,
            with_position=with_position
        )
        return func.HttpResponse(
            json.dumps({"message": f"Full-text search index created on table '{table_name}'"}),
            mimetype="application/json"
        )
    except Exception as e:
        return func.HttpResponse(
            json.dumps({"detail": str(e)}),
            status_code=500,
            mimetype="application/json"
        )

@app.route(route="{table_name}/create_vector_index", methods=["POST"])
def create_vector_index_endpoint(req: func.HttpRequest) -> func.HttpResponse:
    table_name: str = req.route_params.get("table_name")
    try:
        payload = req.get_json()
    except Exception:
        return func.HttpResponse(
            json.dumps({"detail": "Invalid JSON body"}),
            status_code=400,
            mimetype="application/json"
        )
    vector_column_name = payload.get("vector_column_name")
    replace = payload.get("replace", True)
    metric = payload.get("metric", "L2")
    num_partitions = int(payload.get("num_partitions", 256))
    num_sub_vectors = int(payload.get("num_sub_vectors", 96))
    index_type = payload.get("index_type", "IVF_PQ")
    num_bits = int(payload.get("num_bits", 8))
    try:
        repo.create_vector_index(
            table_name,
            vector_column_name=vector_column_name,
            replace=replace,
            metric=metric,
            num_partitions=num_partitions,
            num_sub_vectors=num_sub_vectors,
            index_type=index_type,
            num_bits=num_bits
        )
        return func.HttpResponse(
            json.dumps({"message": f"Vector index created on table '{table_name}'"}),
            mimetype="application/json"
        )
    except Exception as e:
        return func.HttpResponse(
            json.dumps({"detail": str(e)}),
            status_code=500,
            mimetype="application/json"
        )

@app.route(route="create_table", methods=["POST"])    
def create_table_endpoint(req: func.HttpRequest) -> func.HttpResponse:
    try:
        payload = req.get_json()
    except Exception:
        return func.HttpResponse(json.dumps({"detail": "Invalid JSON body"}),
                                 status_code=400,
                                 mimetype="application/json")

    # Get table properties from payload; defaults are provided if keys are missing.
    table_name: str = payload.get("table_name", TABLE_NAME)
    mode: str = payload.get("mode", "create")
    # For now, we are not converting a JSON schema into a pyarrow.Schema.
    schema = None  
    data = payload.get("data")

    try:
        repo.create_table(table_name, schema=schema, data=data, mode=mode)
        return func.HttpResponse(json.dumps({"message": f"Table '{table_name}' created successfully"}),
                                 mimetype="application/json")
    except Exception as e:
        return func.HttpResponse(json.dumps({"detail": str(e)}),
                                 status_code=500,
                                 mimetype="application/json")

@app.route(route="{table_name}/items", methods=["POST"])
def insert_item(req: func.HttpRequest) -> func.HttpResponse:
    table_name: str = req.route_params.get("table_name")
    try:
        item = req.get_json()
    except Exception:
        return func.HttpResponse(json.dumps({"detail": "Invalid JSON body"}),
                                 status_code=400,
                                 mimetype="application/json")
    try:
        # Check if the table exists by trying to get it.
        try:
            repo.get_table(table_name)
        except Exception:
            # Table does not exist – create it using the incoming item
            repo.create_table(table_name, data=[item], mode="create")
            return func.HttpResponse(
                json.dumps({"message": f"Table '{table_name}' created and item inserted successfully"}),
                mimetype="application/json"
            )
        # Table exists – perform normal insert
        repo.insert(table_name, [item])
        return func.HttpResponse(
            json.dumps({"message": "Item inserted successfully"}),
            mimetype="application/json"
        )
    except Exception as e:
        return func.HttpResponse(json.dumps({"detail": str(e)}),
                                 status_code=500,
                                 mimetype="application/json")
       
@app.route(route="{table_name}/bulk_items", methods=["POST"])
def bulk_insert_items(req: func.HttpRequest) -> func.HttpResponse:
    table_name: str = req.route_params.get("table_name")
    try:
        payload = req.get_json()
    except Exception:
        return func.HttpResponse(
            json.dumps({"detail": "Invalid JSON body"}),
            status_code=400,
            mimetype="application/json"
        )
    if not isinstance(payload, list):
        return func.HttpResponse(
            json.dumps({"detail": "Expected a list of items in the JSON body"}),
            status_code=400,
            mimetype="application/json"
        )
    try:
        # Check table existence:
        try:
            repo.get_table(table_name)
        except Exception:
            # Table doesn’t exist – create table with the provided bulk data
            repo.create_table(table_name, data=payload, mode="create")
            return func.HttpResponse(
                json.dumps({"message": f"Table '{table_name}' created and bulk insert completed successfully"}),
                mimetype="application/json"
            )
        # Table exists – perform bulk insert as usual
        repo.bulk_insert(table_name, payload)
        return func.HttpResponse(
            json.dumps({"message": "Bulk insert completed successfully"}),
            mimetype="application/json"
        )
    except Exception as e:
        return func.HttpResponse(
            json.dumps({"detail": str(e)}),
            status_code=500,
            mimetype="application/json"
        )
