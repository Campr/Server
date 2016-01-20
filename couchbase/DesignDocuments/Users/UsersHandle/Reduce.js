function (keys, values, rereduce) {
  var result;

  for (var i = 0; i < values.length; i++) {
    var value = values[i];
    
    if (!result ||
        result.date < value.date ||
        (result.date === value.date && result.version < value.version))
      result = value;
  }

  return result;
}
