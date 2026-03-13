import Browse from './Browse';
import Create from './Create';
import Edit from './Edit';
import Manage from './Manage';

export const pages: Record<string, any> = {
  'Products/Browse': Browse,
  'Products/Manage': Manage,
  'Products/Create': Create,
  'Products/Edit': Edit,
};
