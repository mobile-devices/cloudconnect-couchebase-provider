function (doc, meta) {  
    if(doc.type == "track" && doc){
    
        Object.size = function(obj) {
            var size = 0, key;
            for (key in obj) {
                if (obj.hasOwnProperty(key)) size++;
            }
            return size;
        };
        var final_array = [doc.account];
        emit(final_array.concat(dateToArray(doc.created_at)), Object.size(doc.fields));
    }
}

reduce : _stats