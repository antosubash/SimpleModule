import '@measured/puck/puck.css';
import Editor from '../Views/Editor';
import Manage from '../Views/Manage';
import PagesList from '../Views/PagesList';
import Viewer from '../Views/Viewer';

export const pages: Record<string, any> = {
  'PageBuilder/Manage': Manage,
  'PageBuilder/Editor': Editor,
  'PageBuilder/Viewer': Viewer,
  'PageBuilder/PagesList': PagesList,
};
