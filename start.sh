#!/bin/bash
cd /home/ubuntu/fmp-pricing-update
source venv/bin/activate
git pull
pip install -r requirements.txt
python start.py
# see https://gist.github.com/justinmklam/f13bb53be9bb15ec182b4877c9e9958d
# sync test
