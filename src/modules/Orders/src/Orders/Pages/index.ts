import './index.css';
import Create from './Create';
import Edit from './Edit';
import List from './List';

export const pages: Record<string, any> = {
  'Orders/List': List,
  'Orders/Create': Create,
  'Orders/Edit': Edit,
};
