#!/bin/bash
# only need this if venv folder missing
sudo apt-get update
sudo apt-get install python3 python3-venv python3-dev python3-pip build-essential libssl-dev libffi-dev -y
python3 -m venv venv
source venv/bin/activate
#sudo touch /etc/authbind/byport/80
#sudo chmod 777 /etc/authbind/byport/80
pip install wheel
pip install flask flask_wtf waitress requests wtforms_components python-dateutil simplejson

# Service Setup
sudo cp fmp-pricing-update.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable fmp-pricing-update.service
sudo systemctl start fmp-pricing-update.service
