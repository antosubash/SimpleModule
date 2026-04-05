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
  isVerified: boolean;
}

export interface MarketplacePackageDetail extends MarketplacePackage {
  licenseLink: string;
  versions: MarketplacePackageVersion[];
  dependencies: string[];
  readme: string;
}

export interface MarketplacePackageVersion {
  version: string;
  downloads: number;
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

