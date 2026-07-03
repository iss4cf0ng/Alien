# server.py

from http.server import BaseHTTPRequestHandler, HTTPServer
import json
import base64
import importlib

class BridgeRequestHandler(BaseHTTPRequestHandler):
    
    def log_message(self, format, *args):
        return 

    def do_POST(self):
        # Update routing matrix to support the new 'build' action
        if self.path == "/obfuscate":
            action = "obfuscate"
        elif self.path == "/deobfuscate":
            action = "deobfuscate"
        elif self.path == "/build":
            action = "build"
        else:
            action = None
        
        if not action:
            self.send_response(404)
            self.end_headers()
            return

        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length)
        data = json.loads(post_data.decode('utf-8'))

        script_name = data.get("script_name", "builtin")
        payload = data.get("payload", "") # Acts as the raw input text or basic template
        parameters = data.get("parameters", {})

        try:
            # Dynamic load from the 'obfuscators' folder
            module = importlib.import_module(f"obfuscators.{script_name}")
            func = getattr(module, action)
            result = func(payload, **parameters)

            response_data = {"status": "success", "result": result}
            self.send_response(200)

        except Exception as e:
            response_data = {"status": "error", "message": str(e)}
            self.send_response(500)

        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        self.wfile.write(json.dumps(response_data).encode('utf-8'))


def run(port=8000):
    server_address = ('127.0.0.1', port)
    httpd = HTTPServer(server_address, BridgeRequestHandler)
    print(f"[+] Built-in Obfuscation Bridge running on http://127.0.0.1:{port}")
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        httpd.server_close()

if __name__ == "__main__":
    run()