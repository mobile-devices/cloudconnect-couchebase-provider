function(doc, meta) { 
    if (doc.type == "track" && doc.status) { 
        emit(null, null); 
    } 
}