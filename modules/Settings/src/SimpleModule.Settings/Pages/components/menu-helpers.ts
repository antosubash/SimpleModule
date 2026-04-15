export interface MenuItemDto {
  id: number;
  parentId: number | null;
  label: string;
  url: string | null;
  pageRoute: string | null;
  icon: string;
  cssClass: string | null;
  openInNewTab: boolean;
  isVisible: boolean;
  isHomePage: boolean;
  sortOrder: number;
  children: MenuItemDto[];
}

export function findItem(items: MenuItemDto[], id: number): MenuItemDto | null {
  for (const item of items) {
    if (item.id === id) return item;
    const found = findItem(item.children, id);
    if (found) return found;
  }
  return null;
}

export function getDepth(items: MenuItemDto[], id: number, depth = 0): number {
  for (const item of items) {
    if (item.id === id) return depth;
    const found = getDepth(item.children, id, depth + 1);
    if (found >= 0) return found;
  }
  return -1;
}

export function countItems(items: MenuItemDto[]): number {
  let count = 0;
  for (const item of items) {
    count += 1 + countItems(item.children);
  }
  return count;
}
