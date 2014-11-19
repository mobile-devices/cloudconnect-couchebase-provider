function(doc, meta) { 
    if (doc.type == "track" && doc.imei && doc.status == 0) { 
        emit(doc.imei, null); 
    } 
}