// Auto-generated from [Dto] types — do not edit
export interface CreatePageRequest {
  title: string;
  slug: string;
}

export interface Page {
  id: number;
  title: string;
  slug: string;
  content: string;
  isPublished: boolean;
  order: number;
  createdAt: string;
  updatedAt: string;
}

export interface PageSummary {
  id: number;
  title: string;
  slug: string;
  isPublished: boolean;
  order: number;
  createdAt: string;
  updatedAt: string;
}

export interface UpdatePageContentRequest {
  content: string;
}

export interface UpdatePageRequest {
  title: string;
  slug: string;
  order: number;
  isPublished: boolean;
}

