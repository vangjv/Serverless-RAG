import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
@Injectable({
  providedIn: 'root'
})
export class DocumentUploadService {
  private apiUrl = `${environment.ragBaseURL}/api/DocumentProcessor`;
  constructor(private http: HttpClient) { }

  /**
   * Uploads a document file to the document processor API
   * @param file The file to upload
   * @param orgId The organization ID
   * @param chunkingStrategy Optional chunking strategy settings
   * @param ingestionStrategy Optional ingestion strategy
   * @returns An Observable of the API response
   */
  uploadDocument(
    file: File, 
    orgId: string, 
    chunkingStrategy: object = { strategy: 'pagelevel' }, 
    ingestionStrategy: string = 'hi_res'
  ): Observable<any> {
    const formData = new FormData();
    formData.append('', file, file.name);
    formData.append('orgId', orgId);
    formData.append('chunkingOptions', JSON.stringify(chunkingStrategy));
    formData.append('embeddingPlatform', 'openai');
    formData.append('embeddingModel', 'text-embedding-3-large');
    formData.append('ingestionStrategy', ingestionStrategy);
    return this.http.post(this.apiUrl, formData);
  }
}