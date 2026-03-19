import Clients from './OpenIddict/Clients';
import ClientsCreate from './OpenIddict/ClientsCreate';
import ClientsEdit from './OpenIddict/ClientsEdit';

export const pages: Record<string, any> = {
  'OpenIddictModule/OpenIddict/Clients': Clients,
  'OpenIddictModule/OpenIddict/ClientsCreate': ClientsCreate,
  'OpenIddictModule/OpenIddict/ClientsEdit': ClientsEdit,
};
