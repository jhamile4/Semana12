// Genera una colección reproducible. Ejecutar desde la raíz: node scripts/Build-Postman.mjs
import fs from 'node:fs';
fs.mkdirSync('postman', { recursive: true });
const items = [];
function add(name, method, path, code, body, extra = []) {
  const tests = [`pm.test('HTTP ${code}', () => pm.response.to.have.status(${code}));`, ...extra];
  if (code === 204) tests.push("pm.test('Sin cuerpo', () => pm.expect(pm.response.text()).to.eql(''));");
  if (code === 201) tests.push("pm.test('Location del recurso', () => pm.expect(pm.response.headers.get('Location')).to.match(/api\\//i));");
  items.push({ name: `${String(items.length + 1).padStart(2, '0')} ${name}`, request: {
    method, header: body ? [{ key: 'Content-Type', value: 'application/json' }] : [],
    url: '{{baseUrl}}' + path,
    ...(body ? { body: { mode: 'raw', raw: JSON.stringify(body, null, 2), options: { raw: { language: 'json' } } } } : {})
  }, event: [{ listen: 'test', script: { type: 'text/javascript', exec: tests } }] });
}
const cliente = {nombre:'Cliente Postman', dni:'{{dni}}', telefono:'987654321', email:'taller-{{runId}}@example.com'};
const otroCliente = {...cliente, dni:'{{otroDni}}', email:'otro-taller-{{runId}}@example.com'};
const vehiculo = {placa:'{{placa}}', marca:'Toyota', modelo:'Corolla', anio:2022, kilometraje:35000, idCliente:'{{clienteId}}'};
add('Crear cliente', 'POST', '/api/clientes', 201, cliente, ["pm.collectionVariables.set('clienteId', pm.response.json().idCliente);"]);
add('Crear segundo cliente', 'POST', '/api/clientes', 201, otroCliente, ["pm.collectionVariables.set('otroClienteId', pm.response.json().idCliente);"]);
add('Listar clientes', 'GET', '/api/clientes', 200, null, ["pm.test('Incluye cliente creado', () => pm.expect(pm.response.json().some(x => x.idCliente === Number(pm.collectionVariables.get('clienteId')))).to.be.true);"]);
add('Filtro clientes', 'GET', '/api/clientes?buscar={{dni}}', 200, null, ["pm.test('Filtro DNI', () => pm.expect(pm.response.json().some(x => x.dni === pm.collectionVariables.get('dni'))).to.be.true);"]);
add('Obtener cliente', 'GET', '/api/clientes/{{clienteId}}', 200);
add('Cliente existente sin vehiculos', 'GET', '/api/clientes/{{clienteId}}/vehiculos', 200, null, ["pm.test('Colección vacía', () => pm.expect(pm.response.json()).to.eql([]));"]);
add('DNI duplicado POST', 'POST', '/api/clientes', 409, {...cliente, email:'distinto-{{runId}}@example.com'});
add('Email duplicado POST', 'POST', '/api/clientes', 409, {...cliente, dni:'12345678'});
add('Email mayúsculas duplicado', 'POST', '/api/clientes', 409, {...cliente, dni:'12345678',email:'TALLER-{{runId}}@EXAMPLE.COM'});
add('DNI duplicado PUT', 'PUT', '/api/clientes/{{otroClienteId}}', 409, {...otroCliente,idCliente:'{{otroClienteId}}',dni:'{{dni}}'});
add('Email duplicado PUT', 'PUT', '/api/clientes/{{otroClienteId}}', 409, {...otroCliente,idCliente:'{{otroClienteId}}',email:cliente.email});
add('Actualizar cliente', 'PUT', '/api/clientes/{{clienteId}}', 204, {...cliente,idCliente:'{{clienteId}}',nombre:'Cliente actualizado'});
add('Comprobar actualización cliente', 'GET', '/api/clientes/{{clienteId}}', 200, null, ["pm.test('Nombre persistido', () => pm.expect(pm.response.json().nombre).to.eql('Cliente actualizado'));"]);
add('Cliente inválido', 'POST', '/api/clientes', 400, {...cliente,nombre:'',dni:'abc',email:'invalido'});
add('ID cliente discordante', 'PUT', '/api/clientes/{{clienteId}}', 400, {...cliente,idCliente:0});
add('Crear vehiculo solo FK', 'POST', '/api/vehiculos', 201, vehiculo, ["pm.collectionVariables.set('vehiculoId', pm.response.json().idVehiculo);"]);
add('Listar vehiculos con cliente', 'GET', '/api/vehiculos', 200, null, ["pm.test('Include sin ciclos', () => { const m=pm.response.json().find(x=>x.idVehiculo===Number(pm.collectionVariables.get('vehiculoId'))); pm.expect(m.cliente.idCliente).to.eql(Number(pm.collectionVariables.get('clienteId'))); pm.expect(m.cliente).not.to.have.property('vehiculos'); });"]);
add('Filtro por marca', 'GET', '/api/vehiculos?marca=tOyOtA', 200, null, ["pm.test('Filtro sin distinción de mayúsculas', () => { pm.expect(pm.response.json().length).to.be.above(0); pm.response.json().forEach(v => pm.expect(v.marca.toLowerCase()).to.include('toyota')); });"]);
add('Obtener vehiculo', 'GET', '/api/vehiculos/{{vehiculoId}}', 200);
add('Ruta anidada', 'GET', '/api/clientes/{{clienteId}}/vehiculos', 200, null, ["pm.test('Vehiculo vinculado', () => pm.expect(pm.response.json().some(x=>x.idVehiculo===Number(pm.collectionVariables.get('vehiculoId')))).to.be.true);"]);
add('Eliminar cliente con vehiculos', 'DELETE', '/api/clientes/{{clienteId}}', 409);
add('Actualizar vehiculo', 'PUT', '/api/vehiculos/{{vehiculoId}}', 204, {...vehiculo,idVehiculo:'{{vehiculoId}}',kilometraje:45000});
add('Comprobar actualización vehiculo', 'GET', '/api/vehiculos/{{vehiculoId}}', 200, null, ["pm.test('Kilometraje persistido', () => pm.expect(pm.response.json().kilometraje).to.eql(45000));"]);
add('Placa duplicada POST', 'POST', '/api/vehiculos', 409, vehiculo);
add('Placa duplicada PUT', 'PUT', '/api/vehiculos/{{vehiculoId}}', 409, {...vehiculo,idVehiculo:'{{vehiculoId}}',placa:'ABC-123'});
add('FK inexistente POST', 'POST', '/api/vehiculos', 400, {...vehiculo,idCliente:2147483647});
add('FK inexistente PUT', 'PUT', '/api/vehiculos/{{vehiculoId}}', 400, {...vehiculo,idVehiculo:'{{vehiculoId}}',idCliente:2147483647});
add('Anio y kilometraje invalidos', 'POST', '/api/vehiculos', 400, {...vehiculo,anio:1899,kilometraje:-1});
add('Placa de vehiculo obligatoria', 'POST', '/api/vehiculos', 400, {...vehiculo,placa:''});
add('Nombre cliente demasiado largo', 'POST', '/api/clientes', 400, {...cliente,nombre:'x'.repeat(101)});
add('ID vehiculo discordante', 'PUT', '/api/vehiculos/{{vehiculoId}}', 400, {...vehiculo,idVehiculo:0});
for (const resource of ['clientes','vehiculos']) {
  add(`${resource} GET inexistente`, 'GET', `/api/${resource}/2147483647`, 404);
  add(`${resource} PUT inexistente`, 'PUT', `/api/${resource}/2147483647`, 404,
    resource==='clientes'?{...cliente,idCliente:2147483647}:{...vehiculo,idVehiculo:2147483647});
  add(`${resource} DELETE inexistente`, 'DELETE', `/api/${resource}/2147483647`, 404);
}
add('Ruta anidada cliente inexistente', 'GET', '/api/clientes/2147483647/vehiculos', 404);
add('Filtro sin resultados', 'GET', '/api/vehiculos?marca=SinCoincidencias{{runId}}', 200, null, ["pm.test('Lista vacía', () => pm.expect(pm.response.json()).to.eql([]));"]);
add('Eliminar vehiculo', 'DELETE', '/api/vehiculos/{{vehiculoId}}', 204);
add('Vehiculo eliminado', 'GET', '/api/vehiculos/{{vehiculoId}}', 404);
add('Eliminar cliente liberado', 'DELETE', '/api/clientes/{{clienteId}}', 204);
add('Cliente eliminado', 'GET', '/api/clientes/{{clienteId}}', 404);
add('Eliminar segundo cliente', 'DELETE', '/api/clientes/{{otroClienteId}}', 204);
const collection = {info:{name:'Semana 12 Gestión de Taller Mecánico',schema:'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',description:'Ejecutar completa en orden sobre datos de laboratorio. Crea y elimina sus propios registros. No modifica los datos iniciales.'},
  variable:[{key:'baseUrl',value:'http://localhost:5080'}],
  event:[{listen:'prerequest',script:{type:'text/javascript',exec:[
    "if (pm.info.requestName.startsWith('01 ')) {",
    "const id=Date.now().toString()+Math.floor(Math.random()*100000);",
    "const dni=String(10000000+Math.floor(Math.random()*80000000));",
    "pm.collectionVariables.set('runId',id); pm.collectionVariables.set('dni',dni);",
    "pm.collectionVariables.set('otroDni',String(Number(dni)+1));",
    "const letras=[0,1,2].map(()=>String.fromCharCode(65+Math.floor(Math.random()*26))).join('');",
    "pm.collectionVariables.set('placa',letras+'-'+String(100+Math.floor(Math.random()*900)));",
    "}"
  ]}}],item:items};
fs.writeFileSync('postman/TallerMecanico.postman_collection.json',JSON.stringify(collection,null,2)+'\n');
// Matriz legible para el plan y las capturas, derivada de las mismas solicitudes.
fs.mkdirSync('docs',{recursive:true});
fs.writeFileSync('docs/MATRIZ-PRUEBAS.md','# Matriz de solicitudes Postman\n\nCada caso indica método, ruta y código esperado. Ejecutar en orden con Collection Runner.\n\n'+items.map(i=>{
 const code=i.event[0].script.exec[0].match(/HTTP (\d+)/)[1];
 return `- **${i.name}** — \`${i.request.method} ${i.request.url.replace('{{baseUrl}}','')}\` → **${code}**.`;
}).join('\n')+'\n');
console.log(`${items.length} solicitudes generadas.`);
