document.addEventListener("DOMContentLoaded", function () {
    var ctx = document.getElementById('bookingChart').getContext('2d');

var gradient = ctx.createLinearGradient(0, 0, 0, 400);
gradient.addColorStop(0, 'rgba(54, 162, 235, 0.5)');
gradient.addColorStop(1, 'rgba(54, 162, 235, 0)');

var bookingChart = new Chart(ctx, {
    type: 'line',
data: {
    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
datasets: [{
    label: 'Bookings',
data: [30, 45, 60, 40, 70, 90],
borderColor: 'rgb(54, 162, 235)',
backgroundColor: gradient,
tension: 0.4,
fill: true,
pointBackgroundColor: '#fff',
pointBorderColor: 'rgb(54, 162, 235)',
pointHoverBackgroundColor: 'rgb(54, 162, 235)',
pointHoverBorderColor: '#fff'
            }]
        },
options: {
    responsive: true,
maintainAspectRatio: false,
scales: {
    y: {
    beginAtZero: true
                }
            },
plugins: {
    legend: {
    display: false
                }
            }
        }
    });
});