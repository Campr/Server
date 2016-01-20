function (doc, meta) {
  // This only applies to users with handles.
  if (meta.id.indexOf("user") !== 0 || !doc.handle)
    return;

  // Extract values for reduce. Index by handle.
  emit(doc.handle, {
    doc: meta.id,
    date: doc.updated_at,
    version: doc.version
  });
}
