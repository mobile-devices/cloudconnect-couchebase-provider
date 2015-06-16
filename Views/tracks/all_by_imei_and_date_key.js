function(doc, meta) { 
    if (doc.type == "track" && doc.imei && doc.date_key) { 
        emit([doc.imei, doc.date_key], null); 
    } 
}