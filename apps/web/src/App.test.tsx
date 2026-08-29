import { render, screen } from '@testing-library/react'
import App from './App'

describe('App', () => {
  it('renders the static cold-start-safe introduction', () => {
    render(<App />)

    expect(
      screen.getByRole('heading', {
        name: /verder trainen als een apparaat niet beschikbaar is/i,
      }),
    ).toBeInTheDocument()
  })
})
