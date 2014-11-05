function(doc, meta) { 
    if (doc.type == "notification" && doc) { 
        emit(doc.id, doc); 
    } 
}