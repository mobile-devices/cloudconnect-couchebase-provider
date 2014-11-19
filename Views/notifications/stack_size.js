function(doc, meta) { 
    if (doc.type == "notification" && doc.status == 0) { 
        emit(null, doc.status); 
    } 
}