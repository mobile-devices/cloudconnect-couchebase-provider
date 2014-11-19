function(doc, meta) { 
    if (doc.type == "notification" && doc.status) { 
        emit(doc.status, null); 
    } 
}