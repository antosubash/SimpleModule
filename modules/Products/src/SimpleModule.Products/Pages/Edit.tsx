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
  CardHeader,
  CardTitle,
  Container,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';
import { useState } from 'react';
import { ProductsKeys } from '@/Locales/keys';
import type { Product } from '@/types';

interface Props {
  product: Product;
}

export default function Edit({ product }: Props) {
  const { t } = useTranslation('Products');
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/products/${product.id}`, formData);
  }

  function handleDelete() {
    router.delete(`/products/${product.id}`);
    setShowDeleteDialog(false);
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
            <BreadcrumbPage>{t(ProductsKeys.Edit.Breadcrumb)}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <h1 className="text-2xl font-bold tracking-tight">{t(ProductsKeys.Edit.Title)}</h1>

      <Card>
        <CardContent className="p-4 sm:p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">{t(ProductsKeys.Edit.NameLabel)}</Label>
                <Input id="name" name="name" defaultValue={product.name} required />
              </Field>
              <Field>
                <Label htmlFor="price">{t(ProductsKeys.Edit.PriceLabel)}</Label>
                <Input
                  id="price"
                  name="price"
                  type="number"
                  defaultValue={product.price}
                  required
                  min={0.01}
                  step={0.01}
                />
              </Field>
              <Button type="submit">{t(ProductsKeys.Edit.SaveButton)}</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t(ProductsKeys.Edit.DangerZone)}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-text-muted mb-2 sm:mb-3">
            {t(ProductsKeys.Edit.DeleteWarning)}
          </p>
          <Button variant="danger" onClick={() => setShowDeleteDialog(true)}>
            {t(ProductsKeys.Edit.DeleteButton)}
          </Button>
        </CardContent>
      </Card>

      <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t(ProductsKeys.Edit.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(ProductsKeys.Edit.DeleteDialog.Confirm, { name: product.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setShowDeleteDialog(false)}>
              {t(ProductsKeys.Edit.CancelButton)}
            </Button>
            <Button variant="danger" onClick={handleDelete}>
              {t(ProductsKeys.Edit.DeleteButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Container>
  );
}
