function (doc, meta) {
  // This only applies to posts.
  if (meta.id.indexOf("post") !== 0)
    return;

  // Extract values for reduce.
  emit(doc.id, {
    doc: meta.id,
    date: doc.version.received_at,
    version: doc.version.id
  });
}
