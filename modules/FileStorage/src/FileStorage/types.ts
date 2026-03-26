export interface StoredFile {
  id: number;
  fileName: string;
  storagePath: string;
  contentType: string;
  size: number;
  folder: string | null;
  createdAt: string;
}
