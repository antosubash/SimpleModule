// Auto-generated from [Dto] types — do not edit
export interface AddTagRequest {
  name: string;
}

export interface CreatePageRequest {
  title: string;
  slug: string;
}

export interface CreatePageTemplateRequest {
  name: string;
  content: string;
}

export interface Page {
  id: number;
  title: string;
  slug: string;
  content: string;
  draftContent: string;
  metaDescription: string;
  metaKeywords: string;
  ogImage: string;
  isPublished: boolean;
  order: number;
  createdAt: string;
  updatedAt: string;
  tags: PageTag[];
  deletedAt: string | null;
}

export interface PageSummary {
  id: number;
  title: string;
  slug: string;
  isPublished: boolean;
  hasDraft: boolean;
  order: number;
  createdAt: string;
  updatedAt: string;
  deletedAt: string | null;
  tags: string[];
}

export interface PageTag {
  id: number;
  name: string;
}

export interface PageTemplate {
  id: number;
  name: string;
  content: string;
  createdAt: string;
}

export interface UpdatePageContentRequest {
  content: string;
}

export interface UpdatePageRequest {
  title: string;
  slug: string;
  order: number;
  isPublished: boolean;
  metaDescription: string;
  metaKeywords: string;
  ogImage: string;
}

