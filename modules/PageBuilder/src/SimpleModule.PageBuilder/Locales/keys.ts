export const PageBuilderKeys = {
  Manage: {
    Title: 'Manage.Title',
    Description: 'Manage.Description',
    NewPage: 'Manage.NewPage',
    EmptyTitle: 'Manage.EmptyTitle',
    EmptyDescription: 'Manage.EmptyDescription',
    Table: {
      Title: 'Manage.Table.Title',
      Slug: 'Manage.Table.Slug',
      Status: 'Manage.Table.Status',
      Tags: 'Manage.Table.Tags',
      Updated: 'Manage.Table.Updated',
    },
    Status: {
      Published: 'Manage.Status.Published',
      Unpublished: 'Manage.Status.Unpublished',
      Draft: 'Manage.Status.Draft',
    },
    Tag: {
      AddPlaceholder: 'Manage.Tag.AddPlaceholder',
      AddAriaLabel: 'Manage.Tag.AddAriaLabel',
    },
    Actions: {
      AriaLabel: 'Manage.Actions.AriaLabel',
      SrOnly: 'Manage.Actions.SrOnly',
      Edit: 'Manage.Actions.Edit',
      ViewPage: 'Manage.Actions.ViewPage',
      PreviewDraft: 'Manage.Actions.PreviewDraft',
      Publish: 'Manage.Actions.Publish',
      Unpublish: 'Manage.Actions.Unpublish',
      Delete: 'Manage.Actions.Delete',
    },
    DeleteDialog: {
      Title: 'Manage.DeleteDialog.Title',
      Description: 'Manage.DeleteDialog.Description',
      Cancel: 'Manage.DeleteDialog.Cancel',
      Confirm: 'Manage.DeleteDialog.Confirm',
    },
  },
  Editor: {
    SaveAsTemplate: 'Editor.SaveAsTemplate',
    SaveDraft: 'Editor.SaveDraft',
    Saving: 'Editor.Saving',
    Saved: 'Editor.Saved',
    SaveTemplateDialog: {
      Title: 'Editor.SaveTemplateDialog.Title',
      NameLabel: 'Editor.SaveTemplateDialog.NameLabel',
      NamePlaceholder: 'Editor.SaveTemplateDialog.NamePlaceholder',
      Cancel: 'Editor.SaveTemplateDialog.Cancel',
      Save: 'Editor.SaveTemplateDialog.Save',
    },
    TemplatePicker: {
      Title: 'Editor.TemplatePicker.Title',
      Subtitle: 'Editor.TemplatePicker.Subtitle',
      BlankPage: 'Editor.TemplatePicker.BlankPage',
      Cancel: 'Editor.TemplatePicker.Cancel',
    },
    BackToPages: 'Editor.BackToPages',
  },
  Viewer: {
    DraftBanner: 'Viewer.DraftBanner',
  },
  PagesList: {
    Title: 'PagesList.Title',
    EmptyHeading: 'PagesList.EmptyHeading',
    EmptyDescription: 'PagesList.EmptyDescription',
  },
} as const;
