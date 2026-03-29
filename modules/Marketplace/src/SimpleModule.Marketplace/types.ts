// Auto-generated from [Dto] types — do not edit
export interface MarketplacePackage {
  id: string;
  title: string;
  description: string;
  authors: string;
  icon: string;
  totalDownloads: number;
  tags: string[];
  latestVersion: string;
  projectLink: string;
  category: any;
  isInstalled: boolean;
}

export interface MarketplacePackageDetail {
  id: string;
  title: string;
  description: string;
  authors: string;
  icon: string;
  totalDownloads: number;
  tags: string[];
  latestVersion: string;
  projectLink: string;
  licenseLink: string;
  category: any;
  isInstalled: boolean;
  versions: MarketplacePackageVersion[];
  dependencies: string[];
}

export interface MarketplacePackageVersion {
  version: string;
  downloads: number;
  published: string;
}

export interface MarketplaceSearchResult {
  totalHits: number;
  packages: MarketplacePackage[];
}

export interface MarketplaceSearchRequest {
  query: string;
  category: any | null;
  sortBy: any;
  skip: number;
  take: number;
}

