function (doc, meta) {
  // This only applies to posts.
  if (meta.id.indexOf("userpost") !== 0)
    return;

  // Create the object that will be emitted for every mapping.
  var result = {
    user: doc.user,
    post: doc.id,
    version: doc.version.id,
    date: doc.version.received_at
  };

  // Emit the post Id.
  emit([doc.owner, "id", doc.id], result);

  // First level mappings.
  emit([doc.owner, "type", doc.type, doc.received_at], result);
  emit([doc.owner, "following", doc.from_following, doc.received_at], result);
  emit([doc.owner, "user", doc.user, doc.received_at], result);

  // Second level mappings.
  emit([doc.owner, "type:following", doc.type, doc.from_following, doc.received_at], result);
  emit([doc.owner, "type:user", doc.type, doc.user, doc.received_at], result);

  if (!doc.mentions)
    return;

  // Do all the levels for the mentions here.
  for (var i = 0; i < doc.mentions; i++) {
    var mention = doc.mentions[i];

    emit([doc.owner, "mentioning", mention.user, doc.received_at], result);                                             // Level 1.
    emit([doc.owner, "type:mentioning", doc.type, mention.user, doc.received_at], result);                              // Level 2.
    emit([doc.owner, "following:mentioning", doc.type, mention.user, doc.received_at], result);                         // Level 2.
    emit([doc.owner, "user:mentioning", doc.user, mention.user, doc.received_at], result);                              // Level 2.
    emit([doc.owner, "type:following:mentioning", doc.type, doc.from_following, mention.user, doc.received_at], result);// Level 3.
    emit([doc.owner, "type:user:mentioning", doc.type, doc.user, mention.user, doc.received_at], result);               // Level 3.
  }
}
