function(doc, meta) { 
    if (doc.type == "device" && doc) { 
        emit(doc.id, doc); 
    } 
}