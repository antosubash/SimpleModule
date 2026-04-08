import { router } from '@inertiajs/react';
import {
  Button,
  DataGridPage,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { DATASET_STATUS, FORMAT_NAMES, STATUS_NAMES } from './labels';

interface DatasetSummary {
  id: string;
  name: string;
  originalFileName: string;
  format: number;
  status: number;
  featureCount: number | null;
  sizeBytes: number;
  createdAt: string;
  boundingBox: { minX: number; minY: number; maxX: number; maxY: number } | null;
}

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return `${(bytes / 1024 ** i).toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
}

function statusBadge(status: number): JSX.Element {
  const name = STATUS_NAMES[status] ?? 'Unknown';
  const color =
    status === DATASET_STATUS.Ready
      ? 'text-green-600'
      : status === DATASET_STATUS.Failed
        ? 'text-red-600'
        : status === DATASET_STATUS.Processing
          ? 'text-blue-600'
          : 'text-text-muted';
  return <span className={color}>{name}</span>;
}

interface Props {
  datasets: DatasetSummary[];
}

export default function Browse({ datasets }: Props) {
  return (
    <DataGridPage
      title="Datasets"
      description={`${datasets.length} dataset${datasets.length !== 1 ? 's' : ''}`}
      actions={<Button onClick={() => router.visit('/datasets/upload')}>Upload</Button>}
      data={datasets}
      emptyTitle="No datasets yet"
      emptyDescription="Upload a GIS file to get started."
    >
      {() => (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Format</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Features</TableHead>
              <TableHead>Size</TableHead>
              <TableHead>Created</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {datasets.map((d) => (
              <TableRow
                key={d.id}
                className="cursor-pointer hover:bg-muted/50"
                onClick={() => router.visit(`/datasets/${d.id}`)}
              >
                <TableCell className="font-medium">{d.name}</TableCell>
                <TableCell>{FORMAT_NAMES[d.format] ?? d.format}</TableCell>
                <TableCell>{statusBadge(d.status)}</TableCell>
                <TableCell>{d.featureCount ?? '—'}</TableCell>
                <TableCell>{formatSize(d.sizeBytes)}</TableCell>
                <TableCell>{new Date(d.createdAt).toLocaleString()}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </DataGridPage>
  );
}
