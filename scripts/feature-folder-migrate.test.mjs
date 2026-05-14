// scripts/feature-folder-migrate.test.mjs
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { deriveNamespace, rewriteNamespace } from './feature-folder-migrate.mjs';

test('deriveNamespace: file at project root → assembly name only', () => {
  const ns = deriveNamespace({
    assemblyName: 'SimpleModule.Notifications',
    projectRoot: 'modules/Notifications/src/SimpleModule.Notifications',
    filePath: 'modules/Notifications/src/SimpleModule.Notifications/NotificationsModule.cs',
  });
  assert.equal(ns, 'SimpleModule.Notifications');
});

test('deriveNamespace: nested feature folder → dotted segments', () => {
  const ns = deriveNamespace({
    assemblyName: 'SimpleModule.Notifications',
    projectRoot: 'modules/Notifications/src/SimpleModule.Notifications',
    filePath:
      'modules/Notifications/src/SimpleModule.Notifications/Features/Notifications/List/ListNotificationsEndpoint.cs',
  });
  assert.equal(ns, 'SimpleModule.Notifications.Features.Notifications.List');
});

test('deriveNamespace: Contracts assembly with Features subtree', () => {
  const ns = deriveNamespace({
    assemblyName: 'SimpleModule.Notifications.Contracts',
    projectRoot: 'modules/Notifications/src/SimpleModule.Notifications.Contracts',
    filePath:
      'modules/Notifications/src/SimpleModule.Notifications.Contracts/Features/Notifications/List/QueryNotificationsRequest.cs',
  });
  assert.equal(ns, 'SimpleModule.Notifications.Contracts.Features.Notifications.List');
});

test('deriveNamespace: throws when filePath is outside projectRoot', () => {
  assert.throws(
    () =>
      deriveNamespace({
        assemblyName: 'SimpleModule.Notifications',
        projectRoot: 'modules/Notifications/src/SimpleModule.Notifications',
        filePath: 'modules/Other/SomeFile.cs',
      }),
    /not under project root/,
  );
});

test('rewriteNamespace: replaces existing file-scoped namespace', () => {
  const before = [
    'using System;',
    '',
    'namespace SimpleModule.Notifications.Endpoints.Notifications;',
    '',
    'public class Foo {}',
  ].join('\n');
  const after = rewriteNamespace(before, 'SimpleModule.Notifications.Features.Notifications.List');
  assert.match(after, /^namespace SimpleModule\.Notifications\.Features\.Notifications\.List;$/m);
  assert.doesNotMatch(after, /Endpoints\.Notifications/);
});

test('rewriteNamespace: preserves trailing whitespace and other lines', () => {
  const before = 'namespace A.B;\n\npublic class X {}\n';
  const after = rewriteNamespace(before, 'A.C');
  assert.equal(after, 'namespace A.C;\n\npublic class X {}\n');
});

test('rewriteNamespace: no-op when target equals current', () => {
  const before = 'namespace A.B;\n\npublic class X {}\n';
  const after = rewriteNamespace(before, 'A.B');
  assert.equal(after, before);
});

test('rewriteNamespace: throws on block-scoped namespace (unsupported)', () => {
  const before = 'namespace A.B\n{\n    public class X {}\n}\n';
  assert.throws(
    () => rewriteNamespace(before, 'A.C'),
    /file-scoped namespace declaration/,
  );
});

test('rewriteNamespace: throws when no namespace declaration found', () => {
  const before = 'public class X {}\n';
  assert.throws(() => rewriteNamespace(before, 'A.B'), /no file-scoped namespace/);
});
