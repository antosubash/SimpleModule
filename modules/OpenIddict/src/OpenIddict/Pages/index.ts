import Clients from './OpenIddict/Clients';
import ClientsCreate from './OpenIddict/ClientsCreate';
import ClientsEdit from './OpenIddict/ClientsEdit';

export const pages: Record<string, any> = {
  'OpenIddict/OpenIddict/Clients': Clients,
  'OpenIddict/OpenIddict/ClientsCreate': ClientsCreate,
  'OpenIddict/OpenIddict/ClientsEdit': ClientsEdit,
};
