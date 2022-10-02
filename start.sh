#!/bin/bash
cd /home/ubuntu/fmp-pricing-update
source venv/bin/activate
git pull
python main.py
# see https://gist.github.com/justinmklam/f13bb53be9bb15ec182b4877c9e9958d
# sync test
