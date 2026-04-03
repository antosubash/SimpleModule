import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
  Button,
  Card,
  CardContent,
  Container,
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';
import { ProductsKeys } from '../Locales/keys';

export default function Create() {
  const { t } = useTranslation('Products');
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/products', formData);
  }

  return (
    <Container className="space-y-4 sm:space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/products/manage">Products</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{t(ProductsKeys.Create.Breadcrumb)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">{t(ProductsKeys.Create.Title)}</h1>

      <Card>
        <CardContent className="p-4 sm:p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">{t(ProductsKeys.Create.NameLabel)}</Label>
                <Input
                  id="name"
                  name="name"
                  required
                  placeholder={t(ProductsKeys.Create.NamePlaceholder)}
                />
              </Field>
              <Field>
                <Label htmlFor="price">{t(ProductsKeys.Create.PriceLabel)}</Label>
                <Input
                  id="price"
                  name="price"
                  type="number"
                  required
                  min={0.01}
                  step={0.01}
                  placeholder="0.00"
                />
              </Field>
              <Button type="submit">{t(ProductsKeys.Create.SubmitButton)}</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </Container>
  );
}
