function (doc, meta) {
  // This only applies to users with entities.
  if (meta.id.indexOf("user") !== 0 || !doc.email)
    return;

  // Extract values for reduce. Index by entity.
  emit(doc.email, {
    doc: meta.id,
    date: doc.updated_at,
    version: doc.version
  });
}
