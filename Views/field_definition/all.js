function(doc, meta) { 
    if (doc.type == "field_definition" && doc) { 
        emit(doc.id, null); 
    } 
}