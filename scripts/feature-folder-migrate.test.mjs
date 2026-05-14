// scripts/feature-folder-migrate.test.mjs
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { deriveNamespace } from './feature-folder-migrate.mjs';

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
