function (doc, meta) {
  // This only applies to users with entities.
  if (meta.id.indexOf("user") !== 0 || !doc.entity)
    return;

  // Extract values for reduce. Index by entity.
  emit(doc.entity, {
    doc: meta.id,
    date: doc.updated_at,
    version: doc.version
  });
}
