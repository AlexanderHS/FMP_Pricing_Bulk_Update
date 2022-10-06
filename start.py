from flask import Flask, url_for, render_template, redirect, flash, jsonify, request, send_file
from decimal import Decimal
import os
import math
import shutil
import random
import datetime
import requests
import pandas as pd
import simplejson as json
#import numpy as np
from werkzeug.utils import secure_filename
from werkzeug.datastructures import  FileStorage

from form import UploadForm, ConfirmForm

UPLOAD_FOLDER = '/uploads'
ALLOWED_EXTENSIONS = {'.csv'}
SERVER_PATH = 'http://192.168.0.14:8144'
NOMINAL_TEST_SUBMISSION_USER = 'AlexHS'
DEBUG = False

SUBMISSION_USER_OPTIONS = [
    "-",
    "Bec",
    "Catherine",
    "Roisin",
]

app = Flask(__name__)
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER
app.config['MAX_CONTENT_PATH'] = 20000000 # 20mb = 2*10^7 bytes
Anon = lambda **kwargs: type("Object", (), kwargs)

url_options = [
    "https://lexica.art/api/v1/search?q=woman+at+a+computer+in+wool+top.+promotional+photo+painting",
    "https://lexica.art/api/v1/search?q=Western+australia+landscape",
    "https://lexica.art/api/v1/search?q=middle+earth+landscape",
    "https://lexica.art/api/v1/search?q=tiger+cartoon",
    "https://lexica.art/api/v1/search?q=laproscopic",
    "https://lexica.art/api/v1/search?q=russian+ark",
    "https://lexica.art/api/v1/search?q=old+dog",
]

# to conduct a form submission circumventing input validation an attacker would need this
app.config['SECRET_KEY'] = 'r*hQk^sPkrux9K%q&%YhmbDM4UMntFpqhPJFh6@$yE!rx$!MDPtac%CeApFg!#AjbQ'

@app.route('/download/ListSimple')
def downloadBlankListSimple():
    path = "Simple List Price Example.csv"
    path = os.path.join('examples', path)
    return send_file(path, as_attachment=True)

@app.route('/download/ListComplex')
def downloadBlankListComplex():
    path = "Complex List Price Example.csv"
    path = os.path.join('examples', path)
    return send_file(path, as_attachment=True)

@app.route('/download/CustomerSimple')
def downloadBlankCustomerSimple():
    path = "Simple Customer Price Example.csv"
    path = os.path.join('examples', path)
    return send_file(path, as_attachment=True)

@app.route('/download/CustomerComplex')
def downloadBlankCustomerComplex():
    path = "Complex Customer Price Example.csv"
    path = os.path.join('examples', path)
    return send_file(path, as_attachment=True)

@app.route('/download/ClassSimple')
def downloadBlanklassSimple():
    path = "Simple Class Price Example.csv"
    path = os.path.join('examples', path)
    return send_file(path, as_attachment=True)

@app.route('/download/ClassComplex')
def downloadBlankClassComplex():
    path = "Complex Class Price Example.csv"
    path = os.path.join('examples', path)
    return send_file(path, as_attachment=True)

def test_list(df, confirmed, user):
    items = list()
    for index, row in df.iterrows():
        item = row['itemcode']
        value = row['value']
        currency = None
        if 'currency' in df.columns:
            currency = row['currency']
        start = str(datetime.datetime.today() + datetime.timedelta(days=-1))
        if 'start' in df.columns and not pd.isnull(row['start'] ):
            start = row['start']
        end = None
        if 'end' in df.columns and not pd.isnull(row['end'] ):
            end = row['end']
        if isinstance(start, str):
            start = start.split(' ')[0]
            start = datetime.datetime.strptime(start, "%Y-%m-%d").date()
        if isinstance(end, str):
            end = end.split(' ')[0]
            end = datetime.datetime.strptime(end, "%Y-%m-%d").date()
        if end != None: end = end.strftime("%Y-%m-%d")
        if start != None: start = start.strftime("%Y-%m-%d")
        json = f'{{"MinOrder":1,"ItemCode":"{item}","Value":{value},"CurrencyCode":"{currency}","Start":"{start}","End":"{end}"}}'
        json = json.replace('"None"', 'null')
        json = json.replace('"nan"', 'null')
        items.append(json)
    test = 'true'
    if confirmed:
        test = 'false'
    full_json = f'{{"SubmittedUserName":"{user}","TestOnly":{test},"ListPrices":[{",".join(items)}],"ListPriceBreaks":[],"CustomerPrices":[],"PriceClassPrices":[]}}'
    r = requests.get(SERVER_PATH, data=full_json)
    status = False
    if r.content == b'SUCCESS':
        status = True
    info = r.content
    return status, info

def test_break(df, confirmed, user):
    items = list()
    for index, row in df.iterrows():
        item = row['itemcode']
        value = row['value']
        currency = None
        if 'currency' in df.columns:
            currency = row['currency']
        start = str(datetime.datetime.today() + datetime.timedelta(days=-1))
        if 'start' in df.columns and not pd.isnull(row['start'] ):
            start = row['start']
        end = None
        if 'end' in df.columns and not pd.isnull(row['end'] ):
            end = row['end']
        if isinstance(start, str):
            start = start.split(' ')[0]
            start = datetime.datetime.strptime(start, "%Y-%m-%d").date()
        if isinstance(end, str):
            end = end.split(' ')[0]
            end = datetime.datetime.strptime(end, "%Y-%m-%d").date()
        if end != None: end = end.strftime("%Y-%m-%d")
        if start != None: start = start.strftime("%Y-%m-%d")
        project = None
        if 'project' in df.columns and row['project'] != None and row['project'] != '':
            project = row['project']
        break_qty = 1
        if 'break qty' in df.columns and not pd.isnull(row['break qty']):
            break_qty = row['break qty']
        else:
            break_qty = 0
        break_qty = int(break_qty)
        json = f'{{"BreakQty":{break_qty},"ProjectName":"{project}","ItemCode":"{item}","Value":{value},"CurrencyCode":"{currency}","Start":"{start}","End":"{end}"}}'
        json = json.replace('"None"', 'null')
        json = json.replace('"nan"', 'null')
        items.append(json)
    test = 'true'
    if confirmed:
        test = 'false'
    full_json = f'{{"SubmittedUserName":"{user}","TestOnly":{test},"ListPrices":[],"ListPriceBreaks":[{",".join(items)}],"CustomerPrices":[],"PriceClassPrices":[]}}'
    r = requests.get(SERVER_PATH, data=full_json)
    status = False
    if r.content == b'SUCCESS':
        status = True
    info = r.content
    return status, info

def test_cust(df, confirmed, user):
    items = list()
    for index, row in df.iterrows():
        item = row['itemcode']
        value = row['value']
        customer = row['customer']
        currency = None
        if 'currency' in df.columns:
            currency = row['currency']
        start = str(datetime.datetime.today() + datetime.timedelta(days=-1))
        if 'start' in df.columns and not pd.isnull(row['start'] ):
            start = row['start']
        end = None
        if 'end' in df.columns and not pd.isnull(row['end'] ):
            end = row['end']
        if isinstance(start, str):
            start = start.split(' ')[0]
            start = datetime.datetime.strptime(start, "%Y-%m-%d").date()
        if isinstance(end, str):
            end = end.split(' ')[0]
            end = datetime.datetime.strptime(end, "%Y-%m-%d").date()
        if end != None: end = end.strftime("%Y-%m-%d")
        if start != None: start = start.strftime("%Y-%m-%d")
        project = None
        if 'project' in df.columns and row['project'] != None and row['project'] != '':
            project = row['project']
        if 'break qty' in df.columns and not pd.isnull(row['break qty']):
            break_qty = row['break qty']
        else:
            break_qty = 0
        break_qty = int(break_qty)
        json = f'{{"CustomerCode":"{customer}","BreakQty":{break_qty},"ProjectName":"{project}","ItemCode":"{item}","Value":{value},"CurrencyCode":"{currency}","Start":"{start}","End":"{end}"}}'
        json = json.replace('"None"', 'null')
        json = json.replace('"nan"', 'null')
        items.append(json)
    test = 'true'
    if confirmed:
        test = 'false'
    full_json = f'{{"SubmittedUserName":"{user}","TestOnly":{test},"ListPrices":[],"ListPriceBreaks":[],"CustomerPrices":[{",".join(items)}],"PriceClassPrices":[]}}'
    r = requests.get(SERVER_PATH, data=full_json)
    status = False
    if r.content == b'SUCCESS':
        status = True
    info = r.content
    return status, info

def test_class(df, confirmed, user):
    items = list()
    for index, row in df.iterrows():
        item = row['itemcode']
        value = row['value']
        priceclass = row['priceclass']
        currency = None
        if 'currency' in df.columns:
            currency = row['currency']
        start = str(datetime.datetime.today() + datetime.timedelta(days=-1))
        if 'start' in df.columns and not pd.isnull(row['start'] ):
            start = row['start']
        end = None
        if 'end' in df.columns and not pd.isnull(row['end'] ):
            end = row['end']
        if isinstance(start, str):
            start = start.split(' ')[0]
            start = datetime.datetime.strptime(start, "%Y-%m-%d").date()
        if isinstance(end, str):
            end = end.split(' ')[0]
            end = datetime.datetime.strptime(end, "%Y-%m-%d").date()
        if end != None: end = end.strftime("%Y-%m-%d")
        if start != None: start = start.strftime("%Y-%m-%d")
        project = None
        if 'project' in df.columns and row['project'] != None and row['project'] != '':
            project = row['project']
        if 'break qty' in df.columns and not pd.isnull(row['break qty']):
            break_qty = row['break qty']
        else:
            break_qty = 0
        break_qty = int(break_qty)
        json = f'{{"PriceClassName":"{priceclass}","BreakQty":{break_qty},"ProjectName":"{project}","ItemCode":"{item}","Value":{value},"CurrencyCode":"{currency}","Start":"{start}","End":"{end}"}}'
        json = json.replace('"None"', 'null')
        json = json.replace('"nan"', 'null')
        items.append(json)
    test = 'true'
    if confirmed:
        test = 'false'
    full_json = f'{{"SubmittedUserName":"{user}","TestOnly":{test},"ListPrices":[],"ListPriceBreaks":[],"CustomerPrices":[],"PriceClassPrices":[{",".join(items)}]}}'
    r = requests.get(SERVER_PATH, data=full_json)
    status = False
    if r.content == b'SUCCESS':
        status = True
    info = r.content
    return status, info

def test_against_server(df, type, user, confirmed=False):
    options = {
        'list': test_list,
        'break': test_break,
        'cust': test_cust,
        'class': test_class,
    }
    return options[type](df, confirmed, user)

import locale

def form_file_submit(form: UploadForm, type: str, user: str):
    filename = secure_filename(form.file.data.filename)
    feedback_message = ''
    file_valid = True
    if filename != '':
        file_ext = os.path.splitext(filename)[1]
        if file_ext not in ALLOWED_EXTENSIONS:
            feedback_message += "Upload Failed: File doesn't look right. Did you upload a CSV?"
            file_valid = False
        else:
            if not os.path.exists('uploads'):
                os.mkdir('uploads')
            local_path = os.path.join('uploads', filename)
            form.file.data.save(local_path)
            if DEBUG: flash('File is a CSV and we were able to read it, this is a good start.', 'warning')
            df = pd.read_csv(local_path)
            df = df.loc[:, ~df.columns.str.contains('^Unnamed')] # drop extra columns. e.g. itemcode,value,, -> itemcode,value
            df.dropna(axis = 0, how='all', inplace = True) # drop rows which are all NAN e.g. del ',,,,'
            if 'value' in df.columns:
                df['value']=df.value.map(lambda x: locale.atof(x.strip().strip('$').replace(',', '')))
            if 'Value' in df.columns:
                df['Value']=df.Value.map(lambda x: locale.atof(x.strip().strip('$').replace(',', '')))
            ''
            df.columns = df.columns.str.lower()
            os.remove(local_path)
    if type == 'list' and 'break qty' in df.columns:
        type = 'break'
    options = {
        'list': is_valid_list_csv,
        'break': is_valid_list_break_csv,
        'cust': is_valid_customer_csv,
        'class': is_valid_class_csv,
    }
    file_valid = options[type](df)
    if (file_valid):
        feedback_message += "File passes first set of tests."
    if (file_valid):
        if DEBUG: flash(feedback_message, 'warning')
        passed_server_test, info = test_against_server(df, type, user)
        if passed_server_test:
            if DEBUG: flash('File passes second set of tests.', 'warning')
            flash('File passes all tests. Ready to commit changes.', 'warning')
            if not os.path.exists('pkls'):
                os.mkdir('pkls')
            epoch = datetime.datetime.utcfromtimestamp(0)
            ticks = int((datetime.datetime.now() - epoch).total_seconds() * 1000)
            df.to_pickle(os.path.join('pkls', f'{ticks}.pkl'))
            return url_for('confirm', uid=ticks, type=type)
            # show results
            # get confirmation from user
            # then send to server to commit changes
        else:
            flash(f"{repr(info).replace('Failed to parsed JSON. ', '')}.".replace('b"','').replace('"','').replace('..','.'), 'warning')
    else:
        flash('Processing this CSV has failed. Nothing has been saved or changed.', 'danger')
    return None

import pickle

@app.route("/Confirm/<uid>/<type>", methods=('GET', 'POST'))
def confirm(uid, type):
    print('Got here!')
    temp_file_path = os.path.join('pkls', f'{uid}.pkl')
    if not os.path.exists(temp_file_path):
        flash(f"Error: cannot find expected file '{temp_file_path}'", 'warning')
        return redirect('/')
    temp_file = open(temp_file_path, "rb")
    df = pickle.load(temp_file)
    temp_file.close()
    #os.remove(temp_file_path)
    form = ConfirmForm()
    form.user.choices = SUBMISSION_USER_OPTIONS
    if form.validate_on_submit():
        status, info = test_against_server(df, type, confirmed=True, user=form.user.data)
        if status:
            flash(f'Success. Pricing has been updated.', 'success')
        else:
            flash(f'Status: {status}. info: {info}', 'warning')
        return redirect('/')
    templateData = {
        'title' : 'Ready to Apply Changes',
        'form' : form,
        'image_url' : get_image_url(),
        'df' : df,
        } 
    return render_template('confirm.html', **templateData)

@app.route("/ListPrice/", methods=('GET', 'POST'))
def list_price():
    form = UploadForm()
    if form.validate_on_submit():
        filename = secure_filename(form.file.data.filename)
        if filename != '':
            url = form_file_submit(form, 'list', NOMINAL_TEST_SUBMISSION_USER)
            if url is not None:
                return redirect(url)
    image_url = get_image_url()
    templates = dict()
    templates['Simple List Price Example'] = '/download/ListSimple'
    templates['Complex List Price Example'] = '/download/ListComplex'
    templateData = {
        'title' : 'Update List Price',
        'image_url' : image_url,
        'uploads' : [form],
        'templates' : templates,
        } 
    return render_template('UpdatePrice.html', **templateData)

@app.route("/CustomerPrice/", methods=('GET', 'POST'))
def customer_price():
    form = UploadForm()
    if form.validate_on_submit():
        url = form_file_submit(form, 'cust', NOMINAL_TEST_SUBMISSION_USER)
        if url is not None:
            return redirect(url)
    image_url = get_image_url()
    templates = dict()
    templates['Simple Customer Price Example'] = '/download/CustomerSimple'
    templates['Complex Customer Price Example'] = '/download/CustomerComplex'
    templateData = {
        'title' : 'Update Customer Price',
        'image_url' : image_url,
        'templates' : templates,
        'uploads' : [form],
        } 
    return render_template('UpdatePrice.html', **templateData)

@app.route("/ClassPrice/", methods=('GET', 'POST'))
def class_price():
    form = UploadForm()
    if form.validate_on_submit():
        url = form_file_submit(form, 'class', NOMINAL_TEST_SUBMISSION_USER)
        if url is not None:
            return redirect(url)
    image_url = get_image_url()
    templates = dict()
    templates['Simple Class Price Example'] = '/download/ClassSimple'
    templates['Complex Class Price Example'] = '/download/ClassComplex'
    templateData = {
        'title' : 'Update Class Price',
        'image_url' : image_url,
        'templates' : templates,
        'uploads' : [form],
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
    url = random.choice(url_options)
    r = requests.get(url)
    random_image_url = random.sample(r.json()['images'], 1)[0]['src']
    return random_image_url

def is_valid_file(df, required_columns, allowed_columns, numeric_columns, date_columns):
    for c in required_columns:
        if not c in df.columns:
            flash(f"Missing Column '{c}'. Cannot continue.", 'warning')
            return False
    for c in df['value']:
        try:
            x = float(c)
        except:
            flash(f"Found price of '{c}'. A price must be a number.", 'warning')
            return False
        if x < 0:
            flash(f"Found price of '{c}'. A price cannot be less than zero.", 'warning')
            return False
    for c in df.columns:
        if not c in allowed_columns:
            flash(f"Found column '{c}' but don't know what to do with this so failed. Allowed columns are: {allowed_columns}", 'warning')
            return False
    for c in numeric_columns:
        for index, row in df.iterrows():
            if c in df.columns:
                try:
                    row[c] = float(row[c])
                except:
                    flash(f"Found invalid data in column '{c}': '{row[c]}'. This has to be a number without currency symbols.", 'warning')
                    flash(f"You may want to check this is a valid number and try again.", 'warning')
                    return False
    for c in date_columns:
        if c in df.columns:
            try:
                df[c] = pd.to_datetime(df[c], errors='raise')
            except:
                flash(f"Found invalid data in column '{c}'. These have to all be valid dates.", 'warning')
                flash(f"You may want to check this and try again.", 'warning')
                return False
    for c in required_columns:
        for index, row in df.iterrows():
            value = row[c]
            if value == '' or value == None or value == 'nat' or ((type(value) == float or type(value) == int) and math.isnan(value)) or pd.isnull(value):
                flash(f"Found blanks in column '{c}'. Cannot continue.", 'warning')
                return False
    return True

def is_valid_list_csv(df):
    if DEBUG: flash('Checking if this is a valid list price file...', 'warning')
    required_columns = ['itemcode', 'value']
    allowed_columns = ['itemcode', 'value', 'currency', 'start', 'end']
    numeric_columns = ['value']
    date_columns = ['start', 'end']
    return is_valid_file(df=df, allowed_columns=allowed_columns, required_columns=required_columns, numeric_columns=numeric_columns, date_columns=date_columns)

def is_valid_list_break_csv(df):
    if DEBUG: flash('Checking if this is a valid list price break file...', 'warning')
    required_columns = ['itemcode', 'value', 'break qty']
    allowed_columns = ['itemcode', 'value', 'currency', 'start', 'end', 'project', 'break qty']
    numeric_columns = ['value', 'break qty']
    date_columns = ['start', 'end']
    return is_valid_file(df=df, allowed_columns=allowed_columns, required_columns=required_columns, numeric_columns=numeric_columns, date_columns=date_columns)

def is_valid_class_csv(df):
    if DEBUG: flash('Checking if this is a valid customer price  file...', 'warning')
    required_columns = ['priceclass', 'itemcode', 'value']
    allowed_columns = ['itemcode', 'value', 'currency', 'start', 'end', 'project', 'break qty', 'priceclass']
    numeric_columns = ['value', 'break qty']
    date_columns = ['start', 'end']
    return is_valid_file(df=df, allowed_columns=allowed_columns, required_columns=required_columns, numeric_columns=numeric_columns, date_columns=date_columns)

def is_valid_customer_csv(df):
    if DEBUG: flash('Checking if this is a valid class price file...', 'warning')
    required_columns = ['customer', 'itemcode', 'value']
    allowed_columns = ['itemcode', 'value', 'currency', 'start', 'end', 'project', 'break qty', 'customer']
    numeric_columns = ['value', 'break qty']
    date_columns = ['start', 'end']
    return is_valid_file(df=df, allowed_columns=allowed_columns, required_columns=required_columns, numeric_columns=numeric_columns, date_columns=date_columns)

if __name__ == "__main__":
    if os.name == "posix":
        if os.path.exists('pkls'):
            shutil.rmtree('pkls')
        from waitress import serve
        serve(app, host="0.0.0.0", port=8084)
    if os.name == "nt":
        app.run(host='0.0.0.0', port=8091, debug=True)