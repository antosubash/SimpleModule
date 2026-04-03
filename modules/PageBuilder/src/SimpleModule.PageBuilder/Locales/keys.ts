export const PageBuilderKeys = {
  Editor: {
    BackToPages: 'Editor.BackToPages',
    SaveAsTemplate: 'Editor.SaveAsTemplate',
    SaveDraft: 'Editor.SaveDraft',
    SaveTemplateDialog: {
      Cancel: 'Editor.SaveTemplateDialog.Cancel',
      NameLabel: 'Editor.SaveTemplateDialog.NameLabel',
      NamePlaceholder: 'Editor.SaveTemplateDialog.NamePlaceholder',
      Save: 'Editor.SaveTemplateDialog.Save',
      Title: 'Editor.SaveTemplateDialog.Title',
    },
    Saved: 'Editor.Saved',
    Saving: 'Editor.Saving',
    TemplatePicker: {
      BlankPage: 'Editor.TemplatePicker.BlankPage',
      Cancel: 'Editor.TemplatePicker.Cancel',
      Subtitle: 'Editor.TemplatePicker.Subtitle',
      Title: 'Editor.TemplatePicker.Title',
    },
  },
  Manage: {
    Actions: {
      AriaLabel: 'Manage.Actions.AriaLabel',
      Delete: 'Manage.Actions.Delete',
      Edit: 'Manage.Actions.Edit',
      PreviewDraft: 'Manage.Actions.PreviewDraft',
      Publish: 'Manage.Actions.Publish',
      SrOnly: 'Manage.Actions.SrOnly',
      Unpublish: 'Manage.Actions.Unpublish',
      ViewPage: 'Manage.Actions.ViewPage',
    },
    DeleteDialog: {
      Cancel: 'Manage.DeleteDialog.Cancel',
      Confirm: 'Manage.DeleteDialog.Confirm',
      Description: 'Manage.DeleteDialog.Description',
      Title: 'Manage.DeleteDialog.Title',
    },
    Description: 'Manage.Description',
    EmptyDescription: 'Manage.EmptyDescription',
    EmptyTitle: 'Manage.EmptyTitle',
    NewPage: 'Manage.NewPage',
    Status: {
      Draft: 'Manage.Status.Draft',
      Published: 'Manage.Status.Published',
      Unpublished: 'Manage.Status.Unpublished',
    },
    Table: {
      Slug: 'Manage.Table.Slug',
      Status: 'Manage.Table.Status',
      Tags: 'Manage.Table.Tags',
      Title: 'Manage.Table.Title',
      Updated: 'Manage.Table.Updated',
    },
    Tag: {
      AddAriaLabel: 'Manage.Tag.AddAriaLabel',
      AddPlaceholder: 'Manage.Tag.AddPlaceholder',
    },
    Title: 'Manage.Title',
  },
  PagesList: {
    EmptyDescription: 'PagesList.EmptyDescription',
    EmptyHeading: 'PagesList.EmptyHeading',
    Title: 'PagesList.Title',
  },
  Viewer: {
    DraftBanner: 'Viewer.DraftBanner',
  },
} as const;
