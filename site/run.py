#!/usr/bin/env python3
"""Starts the site on your computer:   python3 site/run.py   then open http://127.0.0.1:8000

Options:  --port 8080     --host 0.0.0.0 (visible on your local network)
"""
import argparse
import logging
import sys
from socketserver import ThreadingMixIn
from wsgiref.simple_server import WSGIRequestHandler, WSGIServer, make_server

from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))

from app import config, main  # noqa: E402


class ThreadedServer(ThreadingMixIn, WSGIServer):
    daemon_threads = True


class QuietHandler(WSGIRequestHandler):
    def log_message(self, fmt, *args):
        if not self.path.startswith("/static/"):
            sys.stderr.write("%s %s\n" % (self.address_string(), fmt % args))


def run():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()
    logging.basicConfig(level=logging.INFO)
    main.init()
    server = make_server(args.host, args.port, main.application, server_class=ThreadedServer, handler_class=QuietHandler)
    print("GemLab démarré : http://{}:{}   (mode {})".format(args.host, args.port, config.ENV))
    print("Arrêt : Ctrl+C. Les e-mails de test s'affichent sur /dev/courriels")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nArrêté.")


if __name__ == "__main__":
    run()
