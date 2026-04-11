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
  title: string;
  slug: string;
  content: string;
  draftContent: string;
  metaDescription: string;
  metaKeywords: string;
  ogImage: string;
  isPublished: boolean;
  order: number;
  tags: PageTag[];
  isDeleted: boolean;
  deletedAt: string | null;
  deletedBy: string;
  version: number;
  createdBy: string;
  updatedBy: string;
  id: number;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
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
  name: string;
  content: string;
  id: number;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
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

