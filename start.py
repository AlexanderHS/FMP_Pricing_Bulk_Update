from flask import Flask, url_for, render_template, redirect, flash, jsonify, request, send_file
from decimal import Decimal
import os
import random
import simplejson as json
import requests
import pandas as pd
from werkzeug.utils import secure_filename
from werkzeug.datastructures import  FileStorage

from form import UploadForm

UPLOAD_FOLDER = '/uploads'
ALLOWED_EXTENSIONS = {'.csv'}

app = Flask(__name__)
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER
app.config['MAX_CONTENT_PATH'] = 20000000 # 20mb = 2*10^7 bytes
Anon = lambda **kwargs: type("Object", (), kwargs)

# to conduct a form submission circumventing input validation an attacker would need this
app.config['SECRET_KEY'] = 'r*hQk^sPkrux9K%q&%YhmbDM4UMntFpqhPJFh6@$yE!rx$!MDPtac%CeApFg!#AjbQ'

@app.route('/download/ListPrice')
def downloadBlankList():
    #For windows you need to use drive name [ex: F:/Example.pdf]
    path = "Blank_List_Price.csv"
    return send_file(path, as_attachment=True)

@app.route('/download/ListPriceBreak')
def downloadBlankListBreak():
    #For windows you need to use drive name [ex: F:/Example.pdf]
    path = "Blank_List_Price_w_Break.csv"
    return send_file(path, as_attachment=True)

@app.route("/ListPrice/", methods=('GET', 'POST'))
def list_price():
    form = UploadForm()
    if form.validate_on_submit():
        filename = secure_filename(form.file.data.filename)
        feedback_message = ''
        file_valid = True
        if filename != '':
            file_ext = os.path.splitext(filename)[1]
            if file_ext not in ALLOWED_EXTENSIONS:
                feedback_message += "Upload Failed: File doesn't look right. Did you upload a CSV?"
                file_vslid = False
            else:
                local_path = 'uploads/' + filename
                form.file.data.save(local_path)
                flash('Finished reading CSV.', 'success')
                df = pd.read_csv(local_path)
                os.remove(local_path)
        if (FileValidation.is_valid_list_csv(df) or FileValidation.is_valid_list_break_csv(df)):
            feedback_message += '<br>'
            feedback_message += "File contents inspected and okay."
        if (file_valid):
            flash(feedback_message, 'success')
        else:
            flash(feedback_message, 'danger')
    image_url = get_image_url()
    templates = dict()
    templates['Blank List Price Template'] = '/download/ListPrice'
    templates['Blank List Price Template (with Price Breaks)'] = '/download/ListPriceBreak'
    templateData = {
        'title' : 'Update List Price',
        'image_url' : image_url,
        'uploads' : [form],
        'templates' : templates,
        } 
    return render_template('UpdatePrice.html', **templateData)

@app.route("/CustomerPrice/", methods=('GET', 'POST'))
def customer_price():
    image_url = get_image_url()
    templateData = {
        'title' : 'Update Customer Price',
        'image_url' : image_url,
        } 
    return render_template('UpdatePrice.html', **templateData)

@app.route("/ClassPrice/", methods=('GET', 'POST'))
def class_price():
    image_url = get_image_url()
    templateData = {
        'title' : 'Update Class Price',
        'image_url' : image_url,
        'uploads' : [1],
        }
    return render_template('UpdatePrice.html', **templateData)

@app.route("/<int:number>", methods=('GET', 'POST'))
def extended_base(number):
    return base_url('bg-warning', number)


@app.route("/", methods=('GET', 'POST'))
def base_url(btn_colour='bg-warning', number=0):
    rand = random.randint(1, 1)
    base_screens = dict()
    base_screens['List Price'] = "/ListPrice/"
    base_screens[' Customer Price'] = "/CustomerPrice/"
    base_screens['Class Price'] = "/ClassPrice/"
    image_url = get_image_url()
    templateData = {
        'title' : 'Pricing Bulk Update Utility',
        'base_screens' : base_screens,
        'btn_colour' : btn_colour,
        'image_url' : image_url,
        'number' : number,
        'rand' : rand,
        } 
    return render_template('home.html', **templateData)

def get_image_url():
    url = "https://lexica.art/api/v1/search?q=woman+at+a+computer+in+wool+top.+promotional+photo+painting"
    r = requests.get(url)
    random_image_url = random.sample(r.json()['images'], 1)[0]['src']
    return random_image_url

if __name__ == "__main__":
    if os.name == "posix":
        from waitress import serve
        serve(app, host="0.0.0.0", port=8083)
    if os.name == "nt":
        app.run(host='0.0.0.0', port=8091, debug=True)

class FileValidation:
    def is_valid_list_csv():
        pass
    
    def is_valid_list_break_csv():
        pass
    
    def is_valid_class_csv():
        pass
    
    def is_valid_customer_csv():
        pass
    