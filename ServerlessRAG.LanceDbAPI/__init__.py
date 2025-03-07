# __init__.py (Azure Functions HTTP trigger)
import logging
import azure.functions as func
from azure.functions import AsgiMiddleware
from main import app  # import your FastAPI instance from main.py

def main(req: func.HttpRequest, context: func.Context) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')
    # AsgiMiddleware will adapt your FastAPI (ASGI) app to the Azure Functions HTTP trigger
    return AsgiMiddleware(app).handle(req, context)
