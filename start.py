from flask import Flask, url_for, render_template, redirect, flash, jsonify, request
from decimal import Decimal
import os
import simplejson as json

app = Flask(__name__)
Anon = lambda **kwargs: type("Object", (), kwargs)

# CONFIGS Unlikely to Change
# to conduct a form submission circumventing input validation an attacker would need this
app.config['SECRET_KEY'] = 'r*hQk^sPkrux9K%q&%YhmbDM4UMntFpqhPJFh6@$yE!rx$!MDPtac%CeApFg!#AjbQ'

@app.route("/", methods=('GET', 'POST'))
def base_url():
    templateData = {
        'title' : 'base',
        } 
    return render_template('home.html', **templateData)

if __name__ == "__main__":
    if os.name == "posix":
        from waitress import serve
        serve(app, host="0.0.0.0", port=8083)
    if os.name == "nt":
        app.run(host='0.0.0.0', port=8091, debug=True)