let formIsDirty = false;

export function setUnsavedChanges(dirty: boolean) {
  formIsDirty = dirty;
}

export function hasUnsavedChanges() {
  return formIsDirty;
}
