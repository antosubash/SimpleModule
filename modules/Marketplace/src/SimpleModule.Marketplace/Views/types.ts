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
  category: string;
  isInstalled: boolean;
}

export interface MarketplacePackageDetail extends MarketplacePackage {
  licenseLink: string;
  versions: MarketplacePackageVersion[];
  dependencies: string[];
}

export interface MarketplacePackageVersion {
  version: string;
  downloads: number;
  published: string;
}
